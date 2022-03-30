﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Schema;
using KS.Fiks.IO.Arkiv.Client.ForenkletArkivering;
using KS.Fiks.IO.Arkiv.Client.Models;
using KS.Fiks.IO.Arkiv.Client.Models.Arkivering.Arkivmelding;
using KS.Fiks.IO.Arkiv.Client.Models.Arkivering.Arkivmeldingkvittering;
using KS.Fiks.IO.Arkiv.Client.Models.Innsyn.Sok;
using KS.Fiks.IO.Arkiv.Client.Models.Metadatakatalog;
using ks.fiks.io.arkivintegrasjon.common.AppSettings;
using ks.fiks.io.arkivintegrasjon.common.FiksIOClient;
using ks.fiks.io.arkivsystem.sample.Generators;
using ks.fiks.io.arkivsystem.sample.Handlers;
using ks.fiks.io.arkivsystem.sample.Helpers;
using ks.fiks.io.arkivsystem.sample.Models;
using ks.fiks.io.arkivsystem.sample.Storage;
using ks.fiks.io.arkivsystem.sample.Validering;
using KS.Fiks.IO.Client;
using KS.Fiks.IO.Client.Models;
using KS.Fiks.IO.Client.Models.Feilmelding;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Serilog;
using EksternNoekkel = KS.Fiks.IO.Arkiv.Client.Models.Arkivering.Arkivmeldingkvittering.EksternNoekkel;

namespace ks.fiks.io.arkivsystem.sample
{
    public class ArkivSimulator : BackgroundService
    {
        public const string TestSessionIdHeader = "testSessionId";
        private FiksIOClient client;
        private readonly AppSettings appSettings;
        private static readonly ILogger Log = Serilog.Log.ForContext(MethodBase.GetCurrentMethod()?.DeclaringType);
        public static SizedDictionary<string, Arkivmelding> _arkivmeldingCache;

        public ArkivSimulator(AppSettings appSettings)
        {
            this.appSettings = appSettings;
            Log.Information("Setter opp FIKS integrasjon for arkivsystem");
            client = FiksIOClientBuilder.CreateFiksIoClient(appSettings);
            _arkivmeldingCache = new SizedDictionary<string, Arkivmelding>(100);
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
            
            if (ArkivintegrasjonMeldingTypeV1.IsArkiveringType(mottatt.Melding.MeldingType))
            {
                HandleArkiveringMelding(mottatt);
            }
            else if (ArkivintegrasjonMeldingTypeV1.IsInnsynType(mottatt.Melding.MeldingType))
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

        private static void HandleInnsynMelding(MottattMeldingArgs mottatt)
        {
            var payloads = new List<IPayload>();

            var melding = mottatt.Melding.MeldingType switch
            {
                ArkivintegrasjonMeldingTypeV1.Sok => SokHandler.HandleMelding(mottatt),
                ArkivintegrasjonMeldingTypeV1.JournalpostHent => JournalpostHentHandler.HandleMelding(mottatt),
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

        private static void HandleArkiveringMelding(MottattMeldingArgs mottatt)
        {
            var payloads = new List<IPayload>();
            var melding = mottatt.Melding.MeldingType switch
            {
                ArkivintegrasjonMeldingTypeV1.Arkivmelding => ArkivmeldingHandler.HandleMelding(mottatt),
                ArkivintegrasjonMeldingTypeV1.ArkivmeldingOppdater => ArkivmeldingOppdaterHandler.HandleMelding(mottatt),
                _ => throw new ArgumentException("Case not handled")
            };

            // Både arkivmelding og arkivmeldingOppdater skal sende en mottatt melding
            mottatt.SvarSender.Ack(); // Ack message to remove it from the queue
            var sendtMottattMelding = mottatt.SvarSender.Svar(ArkivintegrasjonMeldingTypeV1.ArkivmeldingMottatt).Result;
            Log.Information($"Svarmelding {sendtMottattMelding.MeldingId} {sendtMottattMelding.MeldingType} sendt...");
            Log.Information("Melding er mottatt i arkiv ok ......");

            if (melding.ResultatMelding != null) {
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
            Log.Information("Svarmelding meldingId {MeldingId}, meldingType {MeldingType} sendt", sendtMelding.MeldingId,
                sendtMelding.MeldingType);
            Log.Information("Melding er ferdig håndtert i arkiv");
            
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