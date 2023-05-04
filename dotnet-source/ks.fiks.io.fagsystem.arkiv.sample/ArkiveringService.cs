using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using KS.Fiks.Arkiv.Forenklet.Arkivering.V1;
using KS.Fiks.Arkiv.Forenklet.Arkivering.V1.Samples;
using KS.Fiks.ASiC_E;
using KS.Fiks.IO.Arkiv.Client.ForenkletArkivering;
using ks.fiks.io.arkivintegrasjon.common.AppSettings;
using ks.fiks.io.arkivintegrasjon.common.FiksIOClient;
using ks.fiks.io.arkivintegrasjon.common.Helpers;
using KS.Fiks.IO.Client;
using KS.Fiks.IO.Client.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ILogger = Serilog.ILogger;

namespace ks.fiks.io.fagsystem.arkiv.sample
{
    public class ArkiveringService : IHostedService, IDisposable
    {
        private FiksIOClient _client;
        private readonly AppSettings _appSettings;
        private static readonly ILogger Log = Serilog.Log.ForContext(MethodBase.GetCurrentMethod()?.DeclaringType);
        private readonly ArkivmeldingFactory _arkivmeldingFactory;

        public ArkiveringService(AppSettings appSettings)
        {
            this._appSettings = appSettings;
            Log.Information("Setter opp FIKS integrasjon for arkivsystem...");
            _arkivmeldingFactory = new ArkivmeldingFactory();
        }
        
        
        public Task Initialization { get; private set; }
        
        private async Task InitializeAsync()
        {
            _client = await FiksIOClientBuilder.CreateFiksIoClient(_appSettings, new LoggerFactory());
        }

        public void Dispose()
        {
            _client.Dispose();
        }
        
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // await FiksIOClient initialization
            await Initialization;
            
            Log.Information("Fagsystem Service is starting.");

            var accountId = _appSettings.FiksIOConfig.FiksIoAccountId;
            
            _client.NewSubscription(OnReceivedMelding);

            Log.Information("Abonnerer på meldinger på konto " + accountId + " ...");

            SendInngående();

            SendUtgående();

            SendOppdatering();

            SendSok();

            await Task.CompletedTask;
        }

        private void SendSok()
        {
            var receiverId = _appSettings.FiksIOConfig.SendToAccountId; // Receiver id as Guid
            var senderId = _appSettings.FiksIOConfig.FiksIoAccountId; // Sender id as Guid

            var konto = _client.Lookup(new LookupRequest("KOMM:0825", "no.ks.fiks.gi.arkivintegrasjon.innsyn.v1", 3)); //TODO for å finne receiverId
            //Prosess også?

            var messageRequest = new MeldingRequest(
                                      mottakerKontoId: receiverId,
                                      avsenderKontoId: senderId,
                                      meldingType: "no.ks.fiks.gi.arkivintegrasjon.innsyn.sok.v1"); // Message type as string
                                                                                                                                 //Se oversikt over meldingstyper på https://github.com/ks-no/fiks-io-meldingstype-katalog/tree/test/schema

            //Konverterer til arkivmelding xml
            var sok = MessageSamples.SokTittel("tittel*");
            var payload = ArkivmeldingSerializeHelper.Serialize(sok);
            
            //Lager FIKS IO melding
            var payloads = new List<IPayload> {new StringPayload(payload, "sok.xml")};

            //Sender til FIKS IO (arkiv løsning)
            var msg = _client.Send(messageRequest, payloads).Result;
            Log.Information("Melding tittel søk " + msg.MeldingId.ToString() + " sendt..." + msg.MeldingType);
            Log.Information(payload);
        }

        private void SendOppdatering()
        {
            var receiverId = _appSettings.FiksIOConfig.SendToAccountId; // Receiver id as Guid
            var senderId = _appSettings.FiksIOConfig.FiksIoAccountId; // Sender id as Guid

            var konto = _client.Lookup(new LookupRequest("KOMM:0825", "no.ks.fiks.gi.arkivintegrasjon.oppdatering.forenklet.v1", 3)); //TODO for å finne receiverId
            //Prosess også?

            var messageRequest = new MeldingRequest(
                                      mottakerKontoId: receiverId,
                                      avsenderKontoId: senderId,
                                      meldingType: "no.ks.fiks.gi.arkivintegrasjon.oppdatering.basis.oppdatersaksmappe.v1"); // Message type as string
                                                                                                                               //Se oversikt over meldingstyper på https://github.com/ks-no/fiks-io-meldingstype-katalog/tree/test/schema
           
            //Konverterer OppdaterSaksmappe til arkivmelding xml
            var inng = MessageSamples.GetOppdaterSaksmappeAnsvarligPaaFagsystemnoekkel("Fagsystem X", "1234", "Testing Testesen", "id343463346");
            var arkivmelding = _arkivmeldingFactory.GetArkivmelding(inng);
            var payload = ArkivmeldingSerializeHelper.Serialize(arkivmelding);
            
            //Lager FIKS IO melding
            var payloads = new List<IPayload> {new StringPayload(payload, "oppdatersaksmappe.xml")};

            //Sender til FIKS IO (arkiv løsning)
            var msg = _client.Send(messageRequest, payloads).Result;
            Log.Information("Melding OppdaterSaksmappeAnsvarligPaaFagsystemnoekkel " + msg.MeldingId.ToString() + " sendt..." + msg.MeldingType);
            Log.Information(payload);
        }

        private void SendInngående()
        {
            var receiverId = _appSettings.FiksIOConfig.SendToAccountId; // Receiver id as Guid
            var senderId = _appSettings.FiksIOConfig.FiksIoAccountId; // Sender id as Guid

            var konto = _client.Lookup(new LookupRequest("KOMM:0825", "no.ks.fiks.gi.arkivintegrasjon.oppdatering.forenklet.v1", 3)); //TODO for å finne receiverId
            //Prosess også?

            //Fagsystem definerer ønsket struktur
            var inng = new ArkivmeldingForenkletInnkommende
            {
                sluttbrukerIdentifikator = "Fagsystemets brukerid",
                nyInnkommendeJournalpost = new InnkommendeJournalpost
                {
                    tittel = "Bestilling av oppmålingsforretning ...",
                    mottattDato = DateTime.Today,
                    dokumentetsDato = DateTime.Today.AddDays(-2),
                    offentlighetsvurdertDato = DateTime.Today,
                    referanseEksternNoekkelForenklet = 
                        new EksternNoekkelForenklet()
                        {
                            fagsystem = "Fagsystem X", noekkel = "e4712424-883c-4068-9cb7-97ac679d7232"
                        },
                    internMottaker =
                        new List<KorrespondansepartIntern>
                        {
                            new KorrespondansepartIntern()
                            {
                                administrativEnhet = "Oppmålingsetaten",
                                referanseAdministrativEnhet = "b631f24b-48fb-4b5c-838e-6a1f7d56fae2"
                            }
                        },
                    mottaker =
                        new List<KorrespondansepartForenklet>
                        {
                            new KorrespondansepartForenklet()
                            {
                                navn = "Test kommune",
                                enhetsidentifikator =
                                    new Enhetsidentifikator() {organisasjonsnummer = "123456789"},
                                postadresse = new EnkelAdresse()
                                {
                                    adresselinje1 = "Oppmålingsetaten",
                                    adresselinje2 = "Rådhusgate 1",
                                    postnr = "3801",
                                    poststed = "Bø"
                                }
                            }
                        },
                    avsender = new List<KorrespondansepartForenklet>
                    {
                        new KorrespondansepartForenklet()
                        {
                            navn = "Anita Avsender",
                            personid = new Personidentifikator()
                            {
                                personidentifikatorLandkode = "NO", personidentifikatorNr = "12345678901"
                            },
                            postadresse = new EnkelAdresse()
                            {
                                adresselinje1 = "Gate 1", postnr = "3801", poststed = "Bø"
                            }
                        }
                    },
                    hoveddokument =
                        new ForenkletDokument
                        {
                            tittel = "Rekvisisjon av oppmålingsforretning", filnavn = "rekvisisjon.pdf"
                        },
                    vedlegg = new List<ForenkletDokument>
                    {
                        new ForenkletDokument() {tittel = "Vedlegg 1", filnavn = "vedlegg.pdf"}
                    },
                }
            };

            //osv...

            //Konverterer til arkivmelding xml
            var arkivmelding = _arkivmeldingFactory.GetArkivmelding(inng);
            var payload = ArkivmeldingSerializeHelper.Serialize(arkivmelding);

            //Lager FIKS IO melding
            var payloads = new List<IPayload>
            {
                new StringPayload(payload, "innkommendejournalpost.xml"),
                new FilePayload(Path.Combine("samples", "rekvisisjon.pdf")),
                new FilePayload(Path.Combine("samples", "vedlegg.pdf"))
            };

            var messageRequest = new MeldingRequest(
                          mottakerKontoId: receiverId,
                          avsenderKontoId: senderId,
                          meldingType: "no.ks.fiks.gi.arkivintegrasjon.oppdatering.basis.arkivmelding.v1"); // Message type as string
                                                                                                                           //Se oversikt over meldingstyper på https://github.com/ks-no/fiks-io-meldingstype-katalog/tree/test/schema
            //Sender til FIKS IO (arkiv løsning)
            var msg = _client.Send(messageRequest, payloads).Result;
            Log.Information("Melding ny inngående journalpost " + msg.MeldingId.ToString() + " sendt..." + msg.MeldingType + "...med 2" +
                            " vedlegg");
            Log.Information(payload);
        }

        private void SendInngåendeBrukerhistorie3_1()
        {
            var receiverId = _appSettings.FiksIOConfig.SendToAccountId; // Receiver id as Guid
            var senderId = _appSettings.FiksIOConfig.FiksIoAccountId; // Sender id as Guid

            var konto = _client.Lookup(new LookupRequest("KOMM:0825", "no.geointegrasjon.arkiv.oppdatering.basis.v1", 3)); //TODO for å finne receiverId
            //Prosess også?

            var messageRequest = new MeldingRequest(
                                      mottakerKontoId: receiverId,
                                      avsenderKontoId: senderId,
                                      meldingType: "no.geointegrasjon.arkiv.oppdatering.basis.arkivmelding.v1"); // Message type as string
                                                                                                                               //Se oversikt over meldingstyper på https://github.com/ks-no/fiks-io-meldingstype-katalog/tree/test/schema

            //Fagsystem definerer ønsket struktur
            var inng = new ArkivmeldingForenkletInnkommende
            {
                sluttbrukerIdentifikator = "9hs2ir",
                nyInnkommendeJournalpost = new InnkommendeJournalpost
                {
                    tittel = "Startlån søknad(Ref=e4reke, SakId=e4reke)",
                    mottattDato = DateTime.Today,
                    dokumentetsDato = DateTime.Today.AddDays(-2),
                    offentlighetsvurdertDato = DateTime.Today,
                    referanseEksternNoekkelForenklet = 
                        new EksternNoekkelForenklet() {fagsystem = "Fagsystem X", noekkel = "e4reke"},
                    mottaker =
                        new List<KorrespondansepartForenklet>
                        {
                            new KorrespondansepartForenklet()
                            {
                                navn = "Test kommune",
                                enhetsidentifikator =
                                    new Enhetsidentifikator() {organisasjonsnummer = "123456789"},
                                postadresse = new EnkelAdresse()
                                {
                                    adresselinje1 = "Startlån avd",
                                    adresselinje2 = "Rådhusgate 1",
                                    postnr = "3801",
                                    poststed = "Bø"
                                }
                            }
                        },
                    avsender = new List<KorrespondansepartForenklet>
                    {
                        new KorrespondansepartForenklet()
                        {
                            navn = "Anita Søker",
                            personid = new Personidentifikator()
                            {
                                personidentifikatorLandkode = "NO", personidentifikatorNr = "12345678901"
                            },
                            postadresse = new EnkelAdresse()
                            {
                                adresselinje1 = "Gate 1", postnr = "3801", poststed = "Bø"
                            }
                        }
                    },
                    hoveddokument =
                        new ForenkletDokument {tittel = "Søknad om startlån", filnavn = "søknad.pdf"},
                    vedlegg = new List<ForenkletDokument>
                    {
                        new ForenkletDokument() {tittel = "Vedlegg 1", filnavn = "vedlegg.pdf"}
                    }
                }
            };

            //Konverterer til arkivmelding xml
            var arkivmelding = _arkivmeldingFactory.GetArkivmelding(inng);
            var payload = ArkivmeldingSerializeHelper.Serialize(arkivmelding);

            //Lager FIKS IO melding
            var payloads = new List<IPayload>
            {
                new StringPayload(payload, "innkommendejournalpost.xml"),
                new FilePayload(Path.Combine("samples", "søknad.pdf")),
                new FilePayload(Path.Combine("samples", "vedlegg.pdf"))
            };

            //Sender til FIKS IO (arkiv løsning)
            var msg = _client.Send(messageRequest, payloads).Result;
            Log.Information("Melding " + msg.MeldingId.ToString() + " sendt..." + msg.MeldingType + "...med 2 vedlegg");
            Log.Information(payload);

        }

        private void SendUtgående()
        {
            var receiverId = _appSettings.FiksIOConfig.SendToAccountId; // Receiver id as Guid
            var senderId = _appSettings.FiksIOConfig.FiksIoAccountId; // Sender id as Guid

            var konto = _client.Lookup(new LookupRequest("KOMM:0825", "no.ks.fiks.gi.arkivintegrasjon.oppdatering.basis.v1", 3)); //TODO for å finne receiverId
            //Prosess også?

            //Fagsystem definerer ønsket struktur
            var utg = new ArkivmeldingForenkletUtgaaende
            {
                sluttbrukerIdentifikator = "Fagsystemets brukerid",
                nyUtgaaendeJournalpost = new UtgaaendeJournalpost()
            };

            utg.nyUtgaaendeJournalpost.tittel = "Tillatelse til ...";
            utg.nyUtgaaendeJournalpost.referanseEksternNoekkelForenklet = new EksternNoekkelForenklet()
            {
                fagsystem = "Fagsystem X",
                noekkel = "759d7aab-6f41-487d-bdb9-dd177ee887c1"
            };

            utg.nyUtgaaendeJournalpost.internAvsender = new List<KorrespondansepartIntern>
            {
                new KorrespondansepartIntern() { 
                    saksbehandler = "Sigve Saksbehandler",
                    referanseSaksbehandler = "60577438-1f97-4c5f-b254-aa758c8786c4"
                }
            };

            utg.nyUtgaaendeJournalpost.mottaker = new List<KorrespondansepartForenklet>
            {
                new KorrespondansepartForenklet() { 
                    navn = "Mons Mottaker", 
                    postadresse = new EnkelAdresse() { 
                        adresselinje1 = "Gate 1", 
                        postnr = "3801", 
                        poststed = "Bø" } 
                },
                new KorrespondansepartForenklet() { 
                    navn = "Foretak Mottaker",
                    enhetsidentifikator = new Enhetsidentifikator() {
                        organisasjonsnummer = "123456789"
                    },
                    kontaktperson = "Kris Kontakt",
                    postadresse = new EnkelAdresse() { 
                        adresselinje1 = "Forretningsgate 1", 
                        postnr = "3801", 
                        poststed = "Bø" } 
                }
            };

            utg.nyUtgaaendeJournalpost.hoveddokument = new ForenkletDokument
            {
                tittel = "Vedtak om tillatelse til ...",
                filnavn = "vedtak.pdf"
            };

            utg.nyUtgaaendeJournalpost.vedlegg = new List<ForenkletDokument> 
            {
                new ForenkletDokument
                {
                    tittel = "Vedlegg 1",
                    filnavn = "vedlegg1.pdf"
                }
            };

            //osv...

            //Konverterer til arkivmelding xml
            var arkivmelding = _arkivmeldingFactory.GetArkivmelding(utg);
            var payload = ArkivmeldingSerializeHelper.Serialize(arkivmelding);

            //Lager FIKS IO melding
            var payloads = new List<IPayload>
            {
                new StringPayload(payload, "utgaaendejournalpost.xml"),
                new FilePayload(Path.Combine("samples", "vedtak.pdf")),
                new FilePayload(Path.Combine("samples", "vedlegg1.pdf"))
            };

            var messageRequest = new MeldingRequest(
                          mottakerKontoId: receiverId,
                          avsenderKontoId: senderId,
                          meldingType: "no.ks.fiks.gi.arkivintegrasjon.oppdatering.basis.arkivmelding.v1"); // Message type as string
                                                                                                                         //Se oversikt over meldingstyper på https://github.com/ks-no/fiks-io-meldingstype-katalog/tree/test/schema

            //Sender til FIKS IO (arkiv løsning)
            var msg = _client.Send(messageRequest, payloads).Result;
            Log.Information("Melding ny utgående journalpost " + msg.MeldingId.ToString() + " sendt..." + msg.MeldingType + "...med 2 vedlegg");
            Log.Information(payload);

        }

        private void SendUtgåendeUtvidet()
        {
            var receiverId = _appSettings.FiksIOConfig.SendToAccountId; // Receiver id as Guid
            var senderId = _appSettings.FiksIOConfig.FiksIoAccountId; // Sender id as Guid

            var konto = _client.Lookup(new LookupRequest("KOMM:0825", "no.geointegrasjon.arkiv.oppdatering.arkivmelding.v1", 3)); //TODO for å finne receiverId
            //Prosess også?

            var messageRequest = new MeldingRequest(
                                      mottakerKontoId: receiverId,
                                      avsenderKontoId: senderId,
                                      meldingType: "no.geointegrasjon.arkiv.oppdatering.arkivmeldingUtgaaende.v1"); // Message type as string
                                                                                                                             //Se oversikt over meldingstyper på https://github.com/ks-no/fiks-io-meldingstype-katalog/tree/test/schema

            //Fagsystem definerer ønsket struktur
            var utg = new ArkivmeldingForenkletUtgaaende
            {
                sluttbrukerIdentifikator = "Fagsystemets brukerid",
                nyUtgaaendeJournalpost = new UtgaaendeJournalpost
                {
                    referanseEksternNoekkelForenklet= new EksternNoekkelForenklet()
                    {
                        fagsystem = "Fagsystem X", noekkel = Guid.NewGuid().ToString()
                    }
                }
            };

            utg.nyUtgaaendeJournalpost.tittel = "Tillatelse til ...";

            utg.nyUtgaaendeJournalpost.internAvsender = new List<KorrespondansepartIntern>
            {
                new KorrespondansepartIntern() {saksbehandler = "Sigve Saksbehandler"}
            };

            utg.nyUtgaaendeJournalpost.mottaker = new List<KorrespondansepartForenklet>
            {
                new KorrespondansepartForenklet()
                {
                    navn = "Mons Mottaker",
                    postadresse = new EnkelAdresse()
                    {
                        adresselinje1 = "Gate 1", postnr = "3801", poststed = "Bø"
                    }
                },
                new KorrespondansepartForenklet()
                {
                    navn = "Foretak Mottaker",
                    postadresse = new EnkelAdresse()
                    {
                        adresselinje1 = "Forretningsgate 1", postnr = "3801", poststed = "Bø"
                    }
                }
            };

            utg.nyUtgaaendeJournalpost.hoveddokument = new ForenkletDokument
            {
                tittel = "Vedtak om tillatelse til ...", filnavn = "vedtak.pdf"
            };

            utg.nyUtgaaendeJournalpost.vedlegg = new List<ForenkletDokument>();
            var vedlegg1 = new ForenkletDokument {tittel = "Vedlegg 1", filnavn = "vedlegg.pdf"};
            utg.nyUtgaaendeJournalpost.vedlegg.Add(vedlegg1);

            //osv...

            //Konverterer til arkivmelding xml
            var arkivmelding = _arkivmeldingFactory.GetArkivmelding(utg);

            //TODO redigere arkivmelding

            var payload = ArkivmeldingSerializeHelper.Serialize(arkivmelding);

            //Lager FIKS IO melding
            var payloads = new List<IPayload>
            {
                new StringPayload(payload, "utgaaendejournalpost.xml"),
                new FilePayload(Path.Combine("samples", "vedtak.pdf")),
                new FilePayload(Path.Combine("samples", "vedlegg.pdf"))
            };

            //Sender til FIKS IO (arkiv løsning)
            var msg = _client.Send(messageRequest, payloads).Result;
            Log.Information("Melding " + msg.MeldingId.ToString() + " sendt..." + msg.MeldingType + "...med 2 vedlegg");
            Log.Information(payload);

        }
        
        static void OnReceivedMelding(object sender, MottattMeldingArgs fileArgs)
        {
            //Se oversikt over meldingstyper på https://github.com/ks-no/fiks-io-meldingstype-katalog/tree/test/schema

            switch (fileArgs.Melding.MeldingType)
            {
                // Process the message
                case "no.ks.fiks.gi.arkivintegrasjon.mottatt.v1":
                case "no.ks.fiks.gi.arkivintegrasjon.kvittering.v1":
                case "no.ks.fiks.gi.arkivintegrasjon.feil.v1":
                case "no.ks.fiks.gi.arkivintegrasjon.innsyn.sok.resultat.v1":
                    MessagePayloadVerification(fileArgs);
                    break;
                default:
                    Log.Information("Ubehandlet melding i køen " + fileArgs.Melding.MeldingId + " " + fileArgs.Melding.MeldingType);
                    //fileArgs.SvarSender.Ack(); // Ack message to remove it from the queue
                    break;
            }
        }

        private static void MessagePayloadVerification(MottattMeldingArgs fileArgs)
        {
            Log.Information("(Svar på " + fileArgs.Melding.SvarPaMelding + ") Melding " + fileArgs.Melding.MeldingId + " " +
                            fileArgs.Melding.MeldingType + " mottas...");


            if (fileArgs.Melding.HasPayload)
            {
                // Verify that message has payload

                IAsicReader reader = new AsiceReader();
                using (var inputStream = fileArgs.Melding.DecryptedStream.Result)
                using (var asice = reader.Read(inputStream))
                {
                    foreach (var asiceReadEntry in asice.Entries)
                    {
                        using (var entryStream = asiceReadEntry.OpenStream())
                        {
                            var reader1 = new StreamReader(entryStream);
                            var text = reader1.ReadToEnd();
                            Log.Information(text);
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
            }

            Log.Information("Melding er håndtert i fagsystem ok ......");

            fileArgs.SvarSender.Ack(); // Ack message to remove it from the queue
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Log.Information("Arkivering Service is stopping.2");
            return Task.CompletedTask;
        }
    }

    
}

