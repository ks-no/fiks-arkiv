using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Schema;
using System.Xml.Serialization;
using KS.Fiks.ASiC_E;
using KS.Fiks.IO.Arkiv.Client.ForenkletArkivering;
using KS.Fiks.IO.Arkiv.Client.Models;
using KS.Fiks.IO.Arkiv.Client.Models.Arkivering.Arkivmelding;
using KS.Fiks.IO.Arkiv.Client.Models.Arkivering.Arkivmeldingkvittering;
using KS.Fiks.IO.Arkiv.Client.Models.Arkivstruktur;
using KS.Fiks.IO.Arkiv.Client.Models.Innsyn.Sok;
using KS.Fiks.IO.Arkiv.Client.Models.Metadatakatalog;
using KS.Fiks.IO.Arkiv.Client.Sample;
using ks.fiks.io.arkivintegrasjon.common.AppSettings;
using ks.fiks.io.arkivintegrasjon.common.FiksIOClient;
using ks.fiks.io.arkivsystem.sample.Helpers;
using KS.Fiks.IO.Client;
using KS.Fiks.IO.Client.Models;
using KS.Fiks.IO.Client.Models.Feilmelding;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Serilog;
using Mappe = KS.Fiks.IO.Arkiv.Client.Models.Arkivstruktur.Mappe;

namespace ks.fiks.io.arkivsystem.sample
{
    public class ArkivService : BackgroundService
    {
        private FiksIOClient client;
        private readonly AppSettings appSettings;
        private static readonly ILogger Log = Serilog.Log.ForContext(MethodBase.GetCurrentMethod()?.DeclaringType);

        public ArkivService(AppSettings appSettings)
        {
            this.appSettings = appSettings;
            Log.Information("Setter opp FIKS integrasjon for arkivsystem...");
            client = FiksIOClientBuilder.CreateFiksIoClient(appSettings); //CreateFiksIoClient();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Log.Information("Arkiv Service is starting.");
            SubscribeToFiksIOClient();
            await Task.CompletedTask;
        }

        private void OnReceivedMelding(object sender, MottattMeldingArgs mottatt)
        {
            //Se oversikt over meldingstyper på https://github.com/ks-no/fiks-io-meldingstype-katalog/tree/test/schema

            // Process the message
            var arkivmeldingXmlSchemaSet = new XmlSchemaSet();
            arkivmeldingXmlSchemaSet.Add("http://www.arkivverket.no/standarder/noark5/arkivmelding/v2", Path.Combine("Schema", "arkivmelding.xsd"));
            arkivmeldingXmlSchemaSet.Add("http://www.arkivverket.no/standarder/noark5/metadatakatalog/v2", Path.Combine("Schema", "metadatakatalog.xsd"));

            var sokXmlSchemaSet = new XmlSchemaSet();
            sokXmlSchemaSet.Add("http://www.ks.no/standarder/fiks/arkiv/sok/v1", Path.Combine("Schema", "sok.xsd"));

            var xmlValidationErrorOccured = false;

            if (ArkivintegrasjonMeldingTypeV1.IsArkiveringType(mottatt.Melding.MeldingType))
            {
                HandleArkiveringMelding(mottatt, arkivmeldingXmlSchemaSet, xmlValidationErrorOccured);
            }
            else if (ArkivintegrasjonMeldingTypeV1.IsInnsynType(mottatt.Melding.MeldingType))
            {
                HandleInnsynMelding(mottatt, sokXmlSchemaSet, xmlValidationErrorOccured);
            }
            else
            {
                Log.Information("Ukjent melding i køen som avvises " + mottatt.Melding.MeldingId + " " + mottatt.Melding.MeldingType);
                mottatt.SvarSender.Nack(); // Nack message to remove it from the queue
            }
        }

        private static void HandleInnsynMelding(MottattMeldingArgs mottatt, XmlSchemaSet sokXmlSchemaSet,
            bool xmlValidationErrorOccured)
        {
            var validationResult = new List<List<string>>();
            Log.Information("Melding " + mottatt.Melding.MeldingId + " " + mottatt.Melding.MeldingType + " mottas...");

            Sok sok = null;
            
            if (mottatt.Melding.HasPayload)
            { // Verify that message has payload

                IAsicReader reader = new AsiceReader();
                using (var inputStream = mottatt.Melding.DecryptedStream.Result)
                using (var asice = reader.Read(inputStream))
                {
                    foreach (var asiceReadEntry in asice.Entries)
                    {
                        using (var entryStream = asiceReadEntry.OpenStream())
                        {
                            if (asiceReadEntry.FileName.Contains(".xml")) 
                            {
                                    validationResult =  new XmlValidation().ValidateXml(
                                        entryStream,
                                        sokXmlSchemaSet
                                    );
                                    if (validationResult[0].Count > 0)
                                    {
                                        xmlValidationErrorOccured = true;
                                    }
                                    var newEntryStream = asiceReadEntry.OpenStream();
                                    var reader1 = new StreamReader(newEntryStream);
                                    var text = reader1.ReadToEnd();
                                    Log.Information("Parsing sok: {SokText}", text);
                                    if (string.IsNullOrEmpty(text))
                                    {
                                        Log.Error("Tom sok? Text: {Sok}", text);
                                    }

                                    using var textReader = (TextReader) new StringReader(text);
                                        sok = (Sok) new XmlSerializer(typeof (Sok)).Deserialize(textReader);
                            }
                            else
                            {
                                Log.Information("Mottatt vedlegg: {Filename}", asiceReadEntry.FileName);
                            }
                        }
                    }
                }
                if (xmlValidationErrorOccured)
                {
                    var ugyldigforespørsel = new Ugyldigforespørsel
                    {
                        ErrorId = Guid.NewGuid().ToString(),
                        Feilmelding = "Feilmelding:\n" + string.Join("\n ", validationResult[0]),
                        CorrelationId = Guid.NewGuid().ToString()
                    };
                    var errorMessage = mottatt.SvarSender.Svar(FeilmeldingMeldingTypeV1.Ugyldigforespørsel, JsonConvert.SerializeObject(ugyldigforespørsel), "payload.json").Result;
                    Log.Information("Svarmelding {MeldingId} {MeldingType} sendt", errorMessage.MeldingId, errorMessage.MeldingType);
                    mottatt.SvarSender.Ack(); // Ack message to remove it from the queue
                }
            }

            object sokeResultat;
            
            //Lager FIKS IO melding
            var payloads = new List<IPayload>();
            string filename;
            string meldingsType;
            switch (sok.ResponsType )
            {
                case ResponsType.Minimum:
                    filename = "sokeresultat-minimum.xml";
                    meldingsType = ArkivintegrasjonMeldingTypeV1.SokResultatMinimum;
                    sokeResultat = SokeresultatHelper.CreateSokeResultatMinimum();
                    break;
                case ResponsType.Noekler:
                    filename = "sokeresultat-noekler.xml";
                    meldingsType = ArkivintegrasjonMeldingTypeV1.SokResultatNoekler;
                    sokeResultat = SokeresultatHelper.CreateSokeResultatUtvidet();
                    break;
                case ResponsType.Utvidet:
                    filename = "sokeresultat-utvidet.xml";
                    meldingsType = ArkivintegrasjonMeldingTypeV1.SokResultatUtvidet;
                    sokeResultat = SokeresultatHelper.CreateSokeResultatNoekler();
                    break;
                default:
                    filename = "sokeresultat-minimum.xml";
                    meldingsType = ArkivintegrasjonMeldingTypeV1.SokResultatMinimum;
                    sokeResultat = SokeresultatHelper.CreateSokeResultatMinimum();
                    break;
            }
            
            payloads.Add(new StringPayload(ArkivmeldingSerializeHelper.Serialize(sokeResultat), filename));

            mottatt.SvarSender.Ack(); // Ack message to remove it from the queue
            
            var svarmsg = mottatt.SvarSender.Svar(meldingsType, payloads).Result;
            Log.Information("Svarmelding meldingId {MeldingId}, meldingType {MeldingType} sendt",  svarmsg.MeldingId, svarmsg.MeldingType );
            Log.Information("Melding er ferdig håndtert i arkiv");
        }

        private static void HandleArkiveringMelding(MottattMeldingArgs mottatt, XmlSchemaSet arkivmeldingXmlSchemaSet,
            bool xmlValidationErrorOccured)
        {
            var validationResult = new List<List<string>>();
            var deserializedArkivmelding = new Arkivmelding();
            Log.Information($"Melding {mottatt.Melding.MeldingId} {mottatt.Melding.MeldingType} mottas...");

            //TODO håndtere meldingen med ønsket funksjonalitet
            if (mottatt.Melding.HasPayload)
            {
                // Verify that message has payload
                validationResult = ValidereXmlMottattMelding(mottatt, arkivmeldingXmlSchemaSet, ref xmlValidationErrorOccured, validationResult, ref deserializedArkivmelding);

                if (xmlValidationErrorOccured) // Ugyldig forespørsel
                {
                    var ugyldigforespørsel = new Ugyldigforespørsel
                    {
                        ErrorId = Guid.NewGuid().ToString(),
                        Feilmelding = "Feilmelding:\n" + string.Join("\n ", validationResult[0]),
                        CorrelationId = Guid.NewGuid().ToString()
                    };
                    mottatt.SvarSender.Ack(); // Ack message to remove it from the queue
                    var errorMessage = mottatt.SvarSender.Svar(FeilmeldingMeldingTypeV1.Ugyldigforespørsel,
                        JsonConvert.SerializeObject(ugyldigforespørsel), "payload.json").Result;
                    Log.Error($"Svarmelding {errorMessage.MeldingId} {errorMessage.MeldingType} sendt");
                }
                else
                {
                    mottatt.SvarSender.Ack(); // Ack message to remove it from the queue
                    var svarmsg = mottatt.SvarSender.Svar(ArkivintegrasjonMeldingTypeV1.ArkivmeldingMottatt).Result;
                    Log.Information($"Svarmelding {svarmsg.MeldingId} {svarmsg.MeldingType} sendt...");
                    Log.Information("Melding er mottatt i arkiv ok ......");
                }
            }
            else
            {
                // Ugyldig forespørsel
                var ugyldigforespørsel = new Ugyldigforespørsel
                {
                    ErrorId = Guid.NewGuid().ToString(),
                    Feilmelding = "Meldingen mangler innhold",
                    CorrelationId = Guid.NewGuid().ToString()
                };

                mottatt.SvarSender.Ack(); // Ack message to remove it from the queue
                var svarmsg = mottatt.SvarSender.Svar(FeilmeldingMeldingTypeV1.Ugyldigforespørsel,
                    JsonConvert.SerializeObject(ugyldigforespørsel), "payload.json").Result;
                Log.Information($"Svarmelding {svarmsg.MeldingId} {svarmsg.MeldingType} sendt");
            }

            if (xmlValidationErrorOccured) return;

            var kvittering = new ArkivmeldingKvittering();
            kvittering.Tidspunkt = DateTime.Now;
            var isMappe = deserializedArkivmelding?.Mappe?.Count > 0;

            if (isMappe)
            {
                var mp = new SaksmappeKvittering
                {
                    SystemID = new SystemID
                    {
                        Value = Guid.NewGuid().ToString()
                    },
                    OpprettetDato = DateTime.Now,
                    Saksaar =  DateTime.Now.Year.ToString(),
                    Sakssekvensnummer = new Random().Next().ToString()
                };

                kvittering.MappeKvittering.Add(mp);
            }
            else
            {
                var jp = new JournalpostKvittering
                {
                    SystemID = new SystemID
                    {
                        Value = Guid.NewGuid().ToString()
                    },
                    Journalaar = DateTime.Now.Year.ToString(),
                    Journalsekvensnummer = new Random().Next().ToString(),
                    Journalpostnummer = new Random().Next(1, 100).ToString()
                };

                kvittering.RegistreringKvittering.Add(jp);
            }
            //TODO simulerer at arkivet arkiverer og nøkler skal returneres

            var payload = ArkivmeldingSerializeHelper.Serialize(kvittering);

            var svarmsg2 = mottatt.SvarSender.Svar(ArkivintegrasjonMeldingTypeV1.ArkivmeldingKvittering, payload, "arkivmelding-kvittering.xml").Result;
            Log.Information($"Svarmelding {svarmsg2.MeldingId} {svarmsg2.MeldingType} sendt...");
            Log.Information("Arkivering er ok ......");
        }

        private static List<List<string>> ValidereXmlMottattMelding(MottattMeldingArgs mottatt, XmlSchemaSet arkivmeldingXmlSchemaSet,
            ref bool xmlValidationErrorOccured, List<List<string>> validationResult, ref Arkivmelding deserializedArkivmelding)
        {
            IAsicReader reader = new AsiceReader();
            using (var inputStream = mottatt.Melding.DecryptedStream.Result)
            using (var asice = reader.Read(inputStream))
            {
                foreach (var asiceReadEntry in asice.Entries)
                {
                    using (var entryStream = asiceReadEntry.OpenStream())
                    {
                        if (asiceReadEntry.FileName.Contains(".xml")) //TODO regel på navning? alltid arkivmelding.xml?
                        {
                            //TODO validere arkivmelding og evt sende feil om den ikke er ok for arkivering
                            validationResult = new XmlValidation().ValidateXml(
                                entryStream,
                                arkivmeldingXmlSchemaSet
                            );
                            if (validationResult[0].Count > 0)
                            {
                                xmlValidationErrorOccured = true;
                            }
                            
                            var newEntryStream = asiceReadEntry.OpenStream();
                            var reader1 = new StreamReader(newEntryStream);
                            var text = reader1.ReadToEnd();
                            Log.Information("Parsing arkivmelding: {ArkivmeldingText}", text);
                            if (string.IsNullOrEmpty(text))
                            {
                                Log.Error("Tom arkivmelding? Text: {ArkivmeldingText}", text);
                            }
                            deserializedArkivmelding = ArkivmeldingSerializeHelper.DeSerialize(text);
                        }
                        else
                            Log.Information($"Mottatt vedlegg: {asiceReadEntry.FileName}");
                    }
                }

                // Check that all digests declared in the manifest are valid
                if (asice.DigestVerifier.Verification().AllValid)
                {
                    // Do something
                }
                else
                {
                    // Handle error
                }
            }

            return validationResult;
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
