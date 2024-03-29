﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;
using KS.Fiks.Arkiv.Models.V1.Meldingstyper;
using ks.fiks.io.arkivintegrasjon.common.AppSettings;
using ks.fiks.io.arkivintegrasjon.common.FiksIOClient;
using ks.fiks.io.arkivintegrasjon.common.Helpers;
using ks.fiks.io.arkivsystem.sample.Generators;
using ks.fiks.io.arkivsystem.sample.Handlers;
using ks.fiks.io.arkivsystem.sample.Models;
using ks.fiks.io.arkivsystem.sample.Storage;
using KS.Fiks.IO.Client;
using KS.Fiks.IO.Client.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ILogger = Serilog.ILogger;

namespace ks.fiks.io.arkivsystem.sample
{
    public class ArkivSimulator : BackgroundService
    {
        public const string TestSessionIdHeader = "testSessionId";
        public const string ValidatorTestNameHeader = "protokollValidatorTestName";
        private FiksIOClient _client;
        private readonly AppSettings _appSettings;
        private static readonly ILogger Log = Serilog.Log.ForContext(MethodBase.GetCurrentMethod()?.DeclaringType);
        public static SizedDictionary<string, Arkivmelding> ArkivmeldingCache;
        public static Dictionary<string, Arkivmelding> ArkivmeldingProtokollValidatorStorage;
        private readonly JournalpostHentHandler _journalpostHentHandler;
        private readonly MappeHentHandler _mappeHentHandler;
        private readonly SokHandler _sokHandler;
        private readonly ArkivmeldingHandler _arkivmeldingHandler;
        private readonly ArkivmeldingOppdaterHandler _arkivmeldingOppdaterHandler;

        public ArkivSimulator(AppSettings appSettings, ILoggerFactory loggerFactory)
        {
            _appSettings = appSettings;
            Log.Information("Setter opp FIKS integrasjon for arkivsystem");
            
            ArkivmeldingCache = new SizedDictionary<string, Arkivmelding>(100);
            ArkivmeldingProtokollValidatorStorage = new Dictionary<string, Arkivmelding>();
            _journalpostHentHandler = new JournalpostHentHandler();
            _mappeHentHandler = new MappeHentHandler();
            _sokHandler = new SokHandler();
            _arkivmeldingHandler = new ArkivmeldingHandler();
            _arkivmeldingOppdaterHandler = new ArkivmeldingOppdaterHandler();
            InitArkivmeldingStorage();
            Initialization = InitializeAsync(loggerFactory);
        }
        
        public Task Initialization { get; private set; }
        
        private async Task InitializeAsync(ILoggerFactory loggerFactory)
        {
            try
            {
                _client = await FiksIOClientBuilder.CreateFiksIoClient(_appSettings, loggerFactory).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Log.Error($"Startup failed create FiksIO Client: {e.Message}", e);
            }
        }

        /*
         * Fyller opp _arkivmeldingProtokollValidatorStorage med arkivmeldinger som validator trenger for å kunne svare på enkeltstående requests
         * som hent- og oppdater-meldinger
         */
        private void InitArkivmeldingStorage()
        {
            var serializer = new XmlSerializer(typeof(Arkivmelding));
            var directories = Directory.GetDirectories("Xml");
            foreach (var directoryName in directories)
            {
                var xml = File.ReadAllText($"{directoryName}/arkivmelding.xml", Encoding.UTF8);
                using TextReader reader = new StringReader(xml);
                var arkivmelding = (Arkivmelding)serializer.Deserialize(reader);
                var key = directoryName.Split(Path.DirectorySeparatorChar)[1];
                ArkivmeldingProtokollValidatorStorage.Add(key, arkivmelding); // Innkommende meldingId er key
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // await FiksIOClient initialization
            await Initialization;
            
            Log.Information("ArkivSimulator is starting");
            SubscribeToFiksIOClient();
            await Task.CompletedTask;
        }

        private void OnReceivedMelding(object sender, MottattMeldingArgs mottatt)
        {
            Log.Information("Melding med {MeldingId} og meldingstype {MeldingsType} mottas", mottatt.Melding.MeldingId,
                mottatt.Melding.MeldingType);
            
            if (FiksArkivMeldingtype.IsArkiveringType(mottatt.Melding.MeldingType))
            {
                HandleArkiveringMelding(mottatt);
            }
            else if (FiksArkivMeldingtype.IsInnsynType(mottatt.Melding.MeldingType))
            {
                HandleInnsynMelding(mottatt);
            }
            else
            { // Ukjent meldingstype
                Log.Information("Ukjent melding i køen. Sender ugyldigforespørsel {MeldingId} {MeldingType}",
                    mottatt.Melding.MeldingId, mottatt.Melding.MeldingType);
                mottatt.SvarSender.Ack();
                var payloads = new List<IPayload>();
                payloads.Add(
                    new StringPayload(
                        JsonConvert.SerializeObject(FeilmeldingGenerator.CreateUgyldigforespoerselMelding($"Ukjent meldingstype {mottatt.Melding.MeldingType} mottatt")),
                        "feilmelding.xml"));
                mottatt.SvarSender.Svar(FiksArkivMeldingtype.Ugyldigforespørsel, payloads );
            }
        }

        private void HandleInnsynMelding(MottattMeldingArgs mottatt)
        {
            var payloads = new List<IPayload>();
            Melding melding;
            
            try
            {
                melding = mottatt.Melding.MeldingType switch
                {
                    FiksArkivMeldingtype.Sok => _sokHandler.HandleMelding(mottatt),
                    FiksArkivMeldingtype.RegistreringHent => _journalpostHentHandler.HandleMelding(mottatt),
                    FiksArkivMeldingtype.MappeHent => _mappeHentHandler.HandleMelding(mottatt),
                    _ => throw new ArgumentException("Case not handled")
                };
            }
            catch (Exception e)
            {
                melding = new Melding
                {
                    ResultatMelding =
                        FeilmeldingGenerator.CreateUgyldigforespoerselMelding(
                            $"Klarte ikke håndtere innkommende melding. Feilmelding: {e.Message}"),
                    FileName = "feilmelding.xml",
                    MeldingsType = FiksArkivMeldingtype.Ugyldigforespørsel,
                };
            }
 
            payloads.Add(new StringPayload(ArkivmeldingSerializeHelper.Serialize(melding.ResultatMelding),
                    melding.FileName));

            mottatt.SvarSender.Ack(); // Ack message to remove it from the queue

            var sendtMelding = mottatt.SvarSender.Svar(melding.MeldingsType, payloads).Result;
            Log.Information("Svarmelding meldingId {MeldingId}, meldingType {MeldingType} sendt", sendtMelding.MeldingId,
                sendtMelding.MeldingType);
            Log.Information("Melding er ferdig håndtert i arkiv");
        }

        private void HandleArkiveringMelding(MottattMeldingArgs mottatt)
        {
            var payloads = new List<IPayload>();
            List<Melding> meldinger = new List<Melding>();
            
            try
            {
                meldinger = mottatt.Melding.MeldingType switch
                {
                    FiksArkivMeldingtype.ArkivmeldingOpprett => _arkivmeldingHandler.HandleMelding(mottatt),
                    FiksArkivMeldingtype.ArkivmeldingOppdater => _arkivmeldingOppdaterHandler.HandleMelding(mottatt),
                    _ => throw new ArgumentException("Case not handled")
                };
            }
            catch (Exception e)
            {
                meldinger.Add(new Melding
                {
                    ResultatMelding = FeilmeldingGenerator.CreateUgyldigforespoerselMelding($"Klarte ikke håndtere innkommende melding. Feilmelding: {e.Message}"),
                    FileName = "feilmelding.xml",
                    MeldingsType = FiksArkivMeldingtype.Ugyldigforespørsel,
                });
            }

            mottatt.SvarSender.Ack(); // Ack message to remove it from the queue

            foreach (var melding in meldinger)
            {
                if (melding.ResultatMelding != null)
                {
              
                    payloads.Add(new StringPayload(ArkivmeldingSerializeHelper.Serialize(melding.ResultatMelding), melding.FileName));
              
                }

                var sendtMelding = mottatt.SvarSender.Svar(melding.MeldingsType, payloads).Result;
                Log.Information("Svarmelding meldingId {MeldingId}, meldingType {MeldingType} sendt",
                    sendtMelding.MeldingId,
                    sendtMelding.MeldingType);
                
            }
        }

        private void SubscribeToFiksIOClient()
        {
            Log.Information("Starter abonnement for FIKS integrasjon for arkivsystem...");
            var accountId = _appSettings.FiksIOConfig.FiksIoAccountId;
            _client.NewSubscription(OnReceivedMelding);
            Log.Information("Abonnerer på meldinger på konto " + accountId + " ...");
        }
    }
}