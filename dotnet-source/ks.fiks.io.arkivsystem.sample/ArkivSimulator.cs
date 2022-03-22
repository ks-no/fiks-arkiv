using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Schema;
using KS.Fiks.ASiC_E;
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
        private const string TestSessionId = "testSessionId";
        private FiksIOClient client;
        private readonly AppSettings appSettings;
        private static readonly ILogger Log = Serilog.Log.ForContext(MethodBase.GetCurrentMethod()?.DeclaringType);
        private static SizedDictionary<string, Arkivmelding> _arkivmeldingCache;

        public ArkivSimulator(AppSettings appSettings)
        {
            this.appSettings = appSettings;
            Log.Information("Setter opp FIKS integrasjon for arkivsystem...");
            client = FiksIOClientBuilder.CreateFiksIoClient(appSettings); //CreateFiksIoClient();
            _arkivmeldingCache = new SizedDictionary<string, Arkivmelding>(100);
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
            var xmlValidationErrorOccured = false;

            if (ArkivintegrasjonMeldingTypeV1.IsArkiveringType(mottatt.Melding.MeldingType))
            {
                HandleArkiveringMelding(mottatt, xmlValidationErrorOccured);
            }
            else if (ArkivintegrasjonMeldingTypeV1.IsInnsynType(mottatt.Melding.MeldingType))
            {
                HandleInnsynMelding(mottatt, xmlValidationErrorOccured);
            }
            else
            {
                Log.Information("Ukjent melding i køen som avvises " + mottatt.Melding.MeldingId + " " + mottatt.Melding.MeldingType);
                mottatt.SvarSender.Nack(); // Nack message to remove it from the queue
            }
        }

        private static void HandleInnsynMelding(MottattMeldingArgs mottatt, bool xmlValidationErrorOccured)
        {
            var validationResult = new List<List<string>>();
            Log.Information("Melding med {MeldingId} og meldingstype {MeldingsType} mottas", mottatt.Melding.MeldingId, mottatt.Melding.MeldingType);

            var payloads = new List<IPayload>();
            object resultatMelding = null;
            var filename = string.Empty;
            var meldingsType = string.Empty;
            
            switch (mottatt.Melding.MeldingType)
            {
                case ArkivintegrasjonMeldingTypeV1.Sok:
                    
                    var sokXmlSchemaSet = new XmlSchemaSet();
                    sokXmlSchemaSet.Add("http://www.ks.no/standarder/fiks/arkiv/sok/v1", Path.Combine("Schema", "sok.xsd"));

                    var sok = SokHandler.GetPayload(mottatt, sokXmlSchemaSet, out xmlValidationErrorOccured, out validationResult);
                    
                    if(xmlValidationErrorOccured)
                    {
                        SendUgyldigforespoersel(mottatt, validationResult);
                    }
                
                    switch (sok.ResponsType )
                    {
                        case ResponsType.Minimum:
                            filename = "sokeresultat-minimum.xml";
                            meldingsType = ArkivintegrasjonMeldingTypeV1.SokResultatMinimum;
                            resultatMelding = SokeresultatGenerator.CreateSokeResultatMinimum(sok.Respons);
                            break;
                        case ResponsType.Noekler:
                            filename = "sokeresultat-noekler.xml";
                            meldingsType = ArkivintegrasjonMeldingTypeV1.SokResultatNoekler;
                            resultatMelding = SokeresultatGenerator.CreateSokeResultatNoekler();
                            break;
                        case ResponsType.Utvidet:
                            filename = "sokeresultat-utvidet.xml";
                            meldingsType = ArkivintegrasjonMeldingTypeV1.SokResultatUtvidet;
                            resultatMelding = SokeresultatGenerator.CreateSokeResultatUtvidet(sok.Respons);
                            break;
                        default:
                            filename = "sokeresultat-minimum.xml";
                            meldingsType = ArkivintegrasjonMeldingTypeV1.SokResultatMinimum;
                            resultatMelding = SokeresultatGenerator.CreateSokeResultatMinimum(sok.Respons);
                            break;
                    }
                    break;
                
                case ArkivintegrasjonMeldingTypeV1.JournalpostHent:
                    
                    var journalpostHentXmlSchemaSet = new XmlSchemaSet();
                    journalpostHentXmlSchemaSet.Add("http://www.arkivverket.no/standarder/noark5/journalpost/hent/v2", Path.Combine("Schema", "journalpostHent.xsd"));
                    journalpostHentXmlSchemaSet.Add("http://www.arkivverket.no/standarder/noark5/metadatakatalog/v2", Path.Combine("Schema", "metadatakatalog.xsd"));

                    
                    var hentMelding = JournalpostHentHandler.GetPayload(mottatt, journalpostHentXmlSchemaSet, out xmlValidationErrorOccured, out validationResult);
                    
                    if(xmlValidationErrorOccured)
                    {
                        SendUgyldigforespoersel(mottatt, validationResult);
                    }
                    
                    // Hent arkivmelding fra "cache" hvis det er en testSessionId i headere
                    Arkivmelding arkivmelding = null;
                    string testSessionId;
                    if (mottatt.Melding.Headere.TryGetValue(TestSessionId, out testSessionId))
                    {
                        _arkivmeldingCache.TryGetValue(testSessionId, out arkivmelding);
                    }

                    resultatMelding = arkivmelding == null ? JournalpostHentGenerator.Create(hentMelding) : JournalpostHentGenerator.Create(hentMelding, (Journalpost) arkivmelding.Registrering[0]);
                    filename = "resultat.xml";
                    meldingsType = ArkivintegrasjonMeldingTypeV1.JournalpostHentResultat;
                    
                    break;
            }
            
            payloads.Add(new StringPayload(ArkivmeldingSerializeHelper.Serialize(resultatMelding), filename));

            mottatt.SvarSender.Ack(); // Ack message to remove it from the queue
            
            var svarmsg = mottatt.SvarSender.Svar(meldingsType, payloads).Result;
            Log.Information("Svarmelding meldingId {MeldingId}, meldingType {MeldingType} sendt",  svarmsg.MeldingId, svarmsg.MeldingType );
            Log.Information("Melding er ferdig håndtert i arkiv");
        }

        private static void SendUgyldigforespoersel(MottattMeldingArgs mottatt, List<List<string>> validationResult)
        {
            var ugyldigforespørsel = new Ugyldigforespørsel
            {
                ErrorId = Guid.NewGuid().ToString(),
                Feilmelding = "Feilmelding:\n" + string.Join("\n ", validationResult[0]),
                CorrelationId = Guid.NewGuid().ToString()
            };
            var errorMessage = mottatt.SvarSender.Svar(FeilmeldingMeldingTypeV1.Ugyldigforespørsel,
                JsonConvert.SerializeObject(ugyldigforespørsel), "payload.json").Result;
            Log.Information("Svarmelding {MeldingId} {MeldingType} sendt", errorMessage.MeldingId,
                errorMessage.MeldingType);
            mottatt.SvarSender.Ack(); // Ack message to remove it from the queue
        }

        private static void HandleArkiveringMelding(MottattMeldingArgs mottatt, bool xmlValidationErrorOccured)
        {
            var arkivmeldingXmlSchemaSet = new XmlSchemaSet();
            arkivmeldingXmlSchemaSet.Add("http://www.arkivverket.no/standarder/noark5/arkivmelding/v2", Path.Combine("Schema", "arkivmelding.xsd"));
            arkivmeldingXmlSchemaSet.Add("http://www.arkivverket.no/standarder/noark5/metadatakatalog/v2", Path.Combine("Schema", "metadatakatalog.xsd"));

            var validationResult = new List<List<string>>();
            var arkivmelding = new Arkivmelding();
            Log.Information($"Melding {mottatt.Melding.MeldingId} {mottatt.Melding.MeldingType} mottas...");

            //TODO håndtere meldingen med ønsket funksjonalitet
            if (mottatt.Melding.HasPayload)
            {
                // Verify that message has payload
                validationResult = Validator.ValidereXmlMottattMelding(mottatt, arkivmeldingXmlSchemaSet, ref xmlValidationErrorOccured, validationResult, ref arkivmelding);

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
            var isMappe = arkivmelding?.Mappe?.Count > 0;

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
                    Journalpostnummer = new Random().Next(1, 100).ToString(),
                    ReferanseEksternNoekkel = new EksternNoekkel()
                    {
                        Fagsystem = arkivmelding.Registrering[0].ReferanseEksternNoekkel.Fagsystem,
                        Noekkel = arkivmelding.Registrering[0].ReferanseEksternNoekkel.Noekkel
                    }
                };

                kvittering.RegistreringKvittering.Add(jp);
            }
            var payload = ArkivmeldingSerializeHelper.Serialize(kvittering);

            // Lagre arkivmelding i "cache" hvis det er en testSessionId i headere
            string testSessionId;
            if (mottatt.Melding.Headere.TryGetValue(TestSessionId, out testSessionId))
            {
                _arkivmeldingCache.Add(testSessionId, arkivmelding);
            }

            var svarmsg2 = mottatt.SvarSender.Svar(ArkivintegrasjonMeldingTypeV1.ArkivmeldingKvittering, payload, "arkivmelding-kvittering.xml").Result;
            Log.Information($"Svarmelding {svarmsg2.MeldingId} {svarmsg2.MeldingType} sendt...");
            Log.Information("Arkivering er ok ......");
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
