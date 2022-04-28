using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;
using KS.Fiks.Arkiv.Models.V1.Meldingstyper;
using ks.fiks.io.arkivintegrasjon.common.AppSettings;
using ks.fiks.io.arkivintegrasjon.common.FiksIOClient;
using ks.fiks.io.arkivintegrasjon.common.Helpers;
using ks.fiks.io.arkivsystem.sample.Generators;
using ks.fiks.io.arkivsystem.sample.Handlers;
using ks.fiks.io.arkivsystem.sample.Storage;
using KS.Fiks.IO.Client;
using KS.Fiks.IO.Client.Models;
using KS.Fiks.IO.Client.Models.Feilmelding;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Serilog;

namespace ks.fiks.io.arkivsystem.sample
{
    public class ArkivSimulator : BackgroundService
    {
        public const string TestSessionIdHeader = "testSessionId";
        private FiksIOClient client;
        private readonly AppSettings appSettings;
        private static readonly ILogger Log = Serilog.Log.ForContext(MethodBase.GetCurrentMethod()?.DeclaringType);
        public static SizedDictionary<string, Arkivmelding> _arkivmeldingCache;
        private JournalpostHentHandler _journalpostHentHandler;
        private MappeHentHandler _mappeHentHandler;
        private SokHandler _sokHandler;
        private ArkivmeldingHandler _arkivmeldingHandler;
        private ArkivmeldingOppdaterHandler _arkivmeldingOppdaterHandler;

        public ArkivSimulator(AppSettings appSettings)
        {
            this.appSettings = appSettings;
            Log.Information("Setter opp FIKS integrasjon for arkivsystem");
            client = FiksIOClientBuilder.CreateFiksIoClient(appSettings);
            _arkivmeldingCache = new SizedDictionary<string, Arkivmelding>(100);
            _journalpostHentHandler = new JournalpostHentHandler();
            _mappeHentHandler = new MappeHentHandler();
            _sokHandler = new SokHandler();
            _arkivmeldingHandler = new ArkivmeldingHandler();
            _arkivmeldingOppdaterHandler = new ArkivmeldingOppdaterHandler();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Log.Information("ArkivSimulator is starting");
            SubscribeToFiksIOClient();
            await Task.CompletedTask;
        }

        private void OnReceivedMelding(object sender, MottattMeldingArgs mottatt)
        {
            Log.Information("Melding med {MeldingId} og meldingstype {MeldingsType} mottas", mottatt.Melding.MeldingId,
                mottatt.Melding.MeldingType);
            
            if (FiksArkivV1Meldingtype.IsArkiveringType(mottatt.Melding.MeldingType))
            {
                HandleArkiveringMelding(mottatt);
            }
            else if (FiksArkivV1Meldingtype.IsInnsynType(mottatt.Melding.MeldingType))
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
                        "payload.json"));
                mottatt.SvarSender.Svar(FeilmeldingMeldingTypeV1.Ugyldigforespørsel, payloads );
            }
        }

        private void HandleInnsynMelding(MottattMeldingArgs mottatt)
        {
            var payloads = new List<IPayload>();

            var melding = mottatt.Melding.MeldingType switch
            {
                FiksArkivV1Meldingtype.Sok => _sokHandler.HandleMelding(mottatt),
                FiksArkivV1Meldingtype.JournalpostHent => _journalpostHentHandler.HandleMelding(mottatt),
                FiksArkivV1Meldingtype.MappeHent => _mappeHentHandler.HandleMelding(mottatt),
                _ => throw new ArgumentException("Case not handled")
            };

            if (melding.MeldingsType == FeilmeldingMeldingTypeV1.Ugyldigforespørsel)
            {
                payloads.Add(new StringPayload(JsonConvert.SerializeObject(melding.ResultatMelding),
                    melding.FileName));
            }
            else
            {
                payloads.Add(new StringPayload(ArkivmeldingSerializeHelper.Serialize(melding.ResultatMelding),
                    melding.FileName));
            }

            mottatt.SvarSender.Ack(); // Ack message to remove it from the queue

            var sendtMelding = mottatt.SvarSender.Svar(melding.MeldingsType, payloads).Result;
            Log.Information("Svarmelding meldingId {MeldingId}, meldingType {MeldingType} sendt", sendtMelding.MeldingId,
                sendtMelding.MeldingType);
            Log.Information("Melding er ferdig håndtert i arkiv");
        }

        private void HandleArkiveringMelding(MottattMeldingArgs mottatt)
        {
            var payloads = new List<IPayload>();
            var meldinger = mottatt.Melding.MeldingType switch
            {
                FiksArkivV1Meldingtype.Arkivmelding => _arkivmeldingHandler.HandleMelding(mottatt),
                FiksArkivV1Meldingtype.ArkivmeldingOppdater => _arkivmeldingOppdaterHandler.HandleMelding(mottatt),
                _ => throw new ArgumentException("Case not handled")
            };

            mottatt.SvarSender.Ack(); // Ack message to remove it from the queue

            foreach (var melding in meldinger)
            {
                if (melding.ResultatMelding != null)
                {
                    if (melding.MeldingsType == FeilmeldingMeldingTypeV1.Ugyldigforespørsel)
                    {
                        payloads.Add(new StringPayload(JsonConvert.SerializeObject(melding.ResultatMelding),
                            melding.FileName));
                    }
                    else
                    {
                        payloads.Add(new StringPayload(ArkivmeldingSerializeHelper.Serialize(melding.ResultatMelding),
                            melding.FileName));
                    }
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
            var accountId = appSettings.FiksIOConfig.FiksIoAccountId;
            client.NewSubscription(OnReceivedMelding);
            Log.Information("Abonnerer på meldinger på konto " + accountId + " ...");
        }
    }
}