using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using FIKS.eMeldingArkiv.eMeldingForenkletArkiv;
using KS.Fiks.ASiC_E;
using ks.fiks.io.arkivintegrasjon.common.AppSettings;
using ks.fiks.io.arkivintegrasjon.common.FiksIOClient;
using ks.fiks.io.arkivintegrasjon.sample.messages;
using KS.Fiks.IO.Client;
using KS.Fiks.IO.Client.Models;
using ks.fiks.io.fagsystem.arkiv.sample.ForenkletArkivering;
using Microsoft.Extensions.Hosting;

namespace ks.fiks.io.fagsystem.arkiv.sample
{
    public class ArkiveringService : IHostedService, IDisposable
    {
        private readonly FiksIOClient client;
        private readonly AppSettings appSettings;

        public ArkiveringService(AppSettings appSettings)
        {
            this.appSettings = appSettings;
            client = FiksIOClientBuilder.CreateFiksIoClient(appSettings);
        }
        public void Dispose()
        {
            client.Dispose();
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Fagsystem Service is starting.");

            var accountId = appSettings.FiksIOConfig.FiksIoAccountId;
            
            client.NewSubscription(OnReceivedMelding);

            Console.WriteLine("Abonnerer på meldinger på konto " + accountId + " ...");

            SendInngående();

            SendUtgående();

            SendOppdatering();

            SendSok();

            return Task.CompletedTask;
        }

        private void SendSok()
        {
            var receiverId = appSettings.FiksIOConfig.SendToAccountId; // Receiver id as Guid
            var senderId = appSettings.FiksIOConfig.FiksIoAccountId; // Sender id as Guid

            var konto = client.Lookup(new LookupRequest("KOMM:0825", "no.ks.fiks.gi.arkivintegrasjon.innsyn.v1", 3)); //TODO for å finne receiverId
            //Prosess også?

            var messageRequest = new MeldingRequest(
                                      mottakerKontoId: receiverId,
                                      avsenderKontoId: senderId,
                                      meldingType: "no.ks.fiks.gi.arkivintegrasjon.innsyn.sok.v1"); // Message type as string
                                                                                                                                 //Se oversikt over meldingstyper på https://github.com/ks-no/fiks-io-meldingstype-katalog/tree/test/schema

            //Konverterer til arkivmelding xml
            var sok = MessageSamples.SokTittel("tittel*");
            var payload = Arkivintegrasjon.Serialize(sok);
            
            //Lager FIKS IO melding
            var payloads = new List<IPayload> {new StringPayload(payload, "sok.xml")};

            //Sender til FIKS IO (arkiv løsning)
            var msg = client.Send(messageRequest, payloads).Result;
            Console.WriteLine("Melding tittel søk " + msg.MeldingId.ToString() + " sendt..." + msg.MeldingType);
            Console.WriteLine(payload);
        }

        private void SendOppdatering()
        {
            var receiverId = appSettings.FiksIOConfig.SendToAccountId; // Receiver id as Guid
            var senderId = appSettings.FiksIOConfig.FiksIoAccountId; // Sender id as Guid

            var konto = client.Lookup(new LookupRequest("KOMM:0825", "no.ks.fiks.gi.arkivintegrasjon.oppdatering.forenklet.v1", 3)); //TODO for å finne receiverId
            //Prosess også?

            var messageRequest = new MeldingRequest(
                                      mottakerKontoId: receiverId,
                                      avsenderKontoId: senderId,
                                      meldingType: "no.ks.fiks.gi.arkivintegrasjon.oppdatering.basis.oppdatersaksmappe.v1"); // Message type as string
                                                                                                                               //Se oversikt over meldingstyper på https://github.com/ks-no/fiks-io-meldingstype-katalog/tree/test/schema
           
            //Konverterer til arkivmelding xml
            var inng = MessageSamples.GetOppdaterSaksmappeAnsvarligPaaFagsystemnoekkel("Fagsystem X", "1234", "Testing Testesen", "id343463346");
            var arkivmelding = Arkivintegrasjon.ConvertOppdaterSaksmappeToArkivmelding(inng);
            var payload = Arkivintegrasjon.Serialize(arkivmelding);
            //Lager FIKS IO melding
            var payloads = new List<IPayload> {new StringPayload(payload, "oppdatersaksmappe.xml")};

            //Sender til FIKS IO (arkiv løsning)
            var msg = client.Send(messageRequest, payloads).Result;
            Console.WriteLine("Melding OppdaterSaksmappeAnsvarligPaaFagsystemnoekkel " + msg.MeldingId.ToString() + " sendt..." + msg.MeldingType);
            Console.WriteLine(payload);
        }

        private void SendInngående()
        {
            var receiverId = appSettings.FiksIOConfig.SendToAccountId; // Receiver id as Guid
            var senderId = appSettings.FiksIOConfig.FiksIoAccountId; // Sender id as Guid

            var konto = client.Lookup(new LookupRequest("KOMM:0825", "no.ks.fiks.gi.arkivintegrasjon.oppdatering.forenklet.v1", 3)); //TODO for å finne receiverId
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
                    referanseEksternNoekkel =
                        new EksternNoekkel
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
                        new List<Korrespondansepart>
                        {
                            new Korrespondansepart()
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
                    avsender = new List<Korrespondansepart>
                    {
                        new Korrespondansepart()
                        {
                            navn = "Anita Avsender",
                            personid = new Personidentifikator()
                            {
                                personidentifikatorType = "F", personidentifikatorNr = "12345678901"
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
            var arkivmelding = Arkivintegrasjon.ConvertForenkletInnkommendeToArkivmelding(inng);
            var payload = Arkivintegrasjon.Serialize(arkivmelding);

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
            var msg = client.Send(messageRequest, payloads).Result;
            Console.WriteLine("Melding ny inngående journalpost " + msg.MeldingId.ToString() + " sendt..." + msg.MeldingType + "...med 2" +
                " vedlegg");
            Console.WriteLine(payload);
        }

        private void SendInngåendeBrukerhistorie3_1()
        {
            var receiverId = appSettings.FiksIOConfig.SendToAccountId; // Receiver id as Guid
            var senderId = appSettings.FiksIOConfig.FiksIoAccountId; // Sender id as Guid

            var konto = client.Lookup(new LookupRequest("KOMM:0825", "no.geointegrasjon.arkiv.oppdatering.basis.v1", 3)); //TODO for å finne receiverId
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
                    referanseEksternNoekkel =
                        new EksternNoekkel {fagsystem = "Fagsystem X", noekkel = "e4reke"},
                    mottaker =
                        new List<Korrespondansepart>
                        {
                            new Korrespondansepart()
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
                    avsender = new List<Korrespondansepart>
                    {
                        new Korrespondansepart()
                        {
                            navn = "Anita Søker",
                            personid = new Personidentifikator()
                            {
                                personidentifikatorType = "F", personidentifikatorNr = "12345678901"
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
            var arkivmelding = Arkivintegrasjon.ConvertForenkletInnkommendeToArkivmelding(inng);
            var payload = Arkivintegrasjon.Serialize(arkivmelding);

            //Lager FIKS IO melding
            var payloads = new List<IPayload>
            {
                new StringPayload(payload, "innkommendejournalpost.xml"),
                new FilePayload(Path.Combine("samples", "søknad.pdf")),
                new FilePayload(Path.Combine("samples", "vedlegg.pdf"))
            };

            //Sender til FIKS IO (arkiv løsning)
            var msg = client.Send(messageRequest, payloads).Result;
            Console.WriteLine("Melding " + msg.MeldingId.ToString() + " sendt..." + msg.MeldingType + "...med 2 vedlegg");
            Console.WriteLine(payload);

        }

        private void SendUtgående()
        {
            var receiverId = appSettings.FiksIOConfig.SendToAccountId; // Receiver id as Guid
            var senderId = appSettings.FiksIOConfig.FiksIoAccountId; // Sender id as Guid

            var konto = client.Lookup(new LookupRequest("KOMM:0825", "no.ks.fiks.gi.arkivintegrasjon.oppdatering.basis.v1", 3)); //TODO for å finne receiverId
            //Prosess også?

            //Fagsystem definerer ønsket struktur
            var utg = new ArkivmeldingForenkletUtgaaende
            {
                sluttbrukerIdentifikator = "Fagsystemets brukerid",
                nyUtgaaendeJournalpost = new UtgaaendeJournalpost()
            };

            utg.nyUtgaaendeJournalpost.tittel = "Tillatelse til ...";
            utg.nyUtgaaendeJournalpost.referanseEksternNoekkel = new EksternNoekkel
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

            utg.nyUtgaaendeJournalpost.mottaker = new List<Korrespondansepart>
            {
                new Korrespondansepart() { 
                    navn = "Mons Mottaker", 
                    postadresse = new EnkelAdresse() { 
                        adresselinje1 = "Gate 1", 
                        postnr = "3801", 
                        poststed = "Bø" } 
                },
                new Korrespondansepart() { 
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
            var arkivmelding = Arkivintegrasjon.ConvertForenkletUtgaaendeToArkivmelding(utg);
            var payload = Arkivintegrasjon.Serialize(arkivmelding);

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
            var msg = client.Send(messageRequest, payloads).Result;
            Console.WriteLine("Melding ny utgående journalpost " + msg.MeldingId.ToString() + " sendt..." + msg.MeldingType + "...med 2 vedlegg");
            Console.WriteLine(payload);

        }

        private void SendUtgåendeUtvidet()
        {
            var receiverId = appSettings.FiksIOConfig.SendToAccountId; // Receiver id as Guid
            var senderId = appSettings.FiksIOConfig.FiksIoAccountId; // Sender id as Guid

            var konto = client.Lookup(new LookupRequest("KOMM:0825", "no.geointegrasjon.arkiv.oppdatering.arkivmelding.v1", 3)); //TODO for å finne receiverId
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
                    referanseEksternNoekkel = new EksternNoekkel
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

            utg.nyUtgaaendeJournalpost.mottaker = new List<Korrespondansepart>
            {
                new Korrespondansepart()
                {
                    navn = "Mons Mottaker",
                    postadresse = new EnkelAdresse()
                    {
                        adresselinje1 = "Gate 1", postnr = "3801", poststed = "Bø"
                    }
                },
                new Korrespondansepart()
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
            var arkivmelding = Arkivintegrasjon.ConvertForenkletUtgaaendeToArkivmelding(utg);

            //TODO redigere arkivmelding

            var payload = Arkivintegrasjon.Serialize(arkivmelding);

            //Lager FIKS IO melding
            var payloads = new List<IPayload>
            {
                new StringPayload(payload, "utgaaendejournalpost.xml"),
                new FilePayload(Path.Combine("samples", "vedtak.pdf")),
                new FilePayload(Path.Combine("samples", "vedlegg.pdf"))
            };

            //Sender til FIKS IO (arkiv løsning)
            var msg = client.Send(messageRequest, payloads).Result;
            Console.WriteLine("Melding " + msg.MeldingId.ToString() + " sendt..." + msg.MeldingType + "...med 2 vedlegg");
            Console.WriteLine(payload);

        }
        
        static void OnReceivedMelding(object sender, MottattMeldingArgs fileArgs)
        {
            //Se oversikt over meldingstyper på https://github.com/ks-no/fiks-io-meldingstype-katalog/tree/test/schema

            // Process the message
            if (fileArgs.Melding.MeldingType == "no.ks.fiks.gi.arkivintegrasjon.mottatt.v1")
            {
                Console.WriteLine("(Svar på " + fileArgs.Melding.SvarPaMelding + ") Melding " + fileArgs.Melding.MeldingId + " " + fileArgs.Melding.MeldingType + " mottas...");

                //TODO håndtere meldingen med ønsket funksjonalitet
                if (fileArgs.Melding.HasPayload)
                { // Verify that message has payload

                    IAsicReader reader = new AsiceReader();
                    using (var inputStream = fileArgs.Melding.DecryptedStream.Result)
                    using (var asice = reader.Read(inputStream))
                    {
                        foreach (var asiceReadEntry in asice.Entries)
                        {

                            using (var entryStream = asiceReadEntry.OpenStream())
                            {
                                StreamReader reader1 = new StreamReader(entryStream);
                                string text = reader1.ReadToEnd();
                                Console.WriteLine(text);
                            }
                        }
                    }
                }
                Console.WriteLine("Melding er håndtert i fagsystem ok ......");

                fileArgs.SvarSender.Ack(); // Ack message to remove it from the queue

            }
            else if (fileArgs.Melding.MeldingType == "no.ks.fiks.gi.arkivintegrasjon.kvittering.v1")
            {
                Console.WriteLine("(Svar på " + fileArgs.Melding.SvarPaMelding + ") Melding " + fileArgs.Melding.MeldingId + " " + fileArgs.Melding.MeldingType + " mottas...");

                //TODO håndtere meldingen med ønsket funksjonalitet
                if (fileArgs.Melding.HasPayload)
                { // Verify that message has payload

                    IAsicReader reader = new AsiceReader();
                    using (var inputStream = fileArgs.Melding.DecryptedStream.Result)
                    using (var asice = reader.Read(inputStream))
                    {
                        foreach (var asiceReadEntry in asice.Entries)
                        {

                            using (var entryStream = asiceReadEntry.OpenStream())
                            {
                                StreamReader reader1 = new StreamReader(entryStream);
                                string text = reader1.ReadToEnd();
                                Console.WriteLine(text);
                            }
                        }
                    }
                }
                Console.WriteLine("Melding er håndtert i fagsystem ok ......");

                fileArgs.SvarSender.Ack(); // Ack message to remove it from the queue

            }
            else if (fileArgs.Melding.MeldingType == "no.ks.fiks.gi.arkivintegrasjon.feil.v1")
            {
                Console.WriteLine("(Svar på " + fileArgs.Melding.SvarPaMelding + ") Melding " + fileArgs.Melding.MeldingId + " " + fileArgs.Melding.MeldingType + " mottas...");

                //TODO håndtere meldingen med ønsket funksjonalitet
                if (fileArgs.Melding.HasPayload)
                { // Verify that message has payload

                    IAsicReader reader = new AsiceReader();
                    using (var inputStream = fileArgs.Melding.DecryptedStream.Result)
                    using (var asice = reader.Read(inputStream))
                    {
                        foreach (var asiceReadEntry in asice.Entries)
                        {

                            using (var entryStream = asiceReadEntry.OpenStream())
                            {
                                StreamReader reader1 = new StreamReader(entryStream);
                                string text = reader1.ReadToEnd();
                                Console.WriteLine(text);
                            }
                        }
                    }
                }
                Console.WriteLine("Melding er håndtert i fagsystem ok ......");

                fileArgs.SvarSender.Ack(); // Ack message to remove it from the queue

            }
            else if (fileArgs.Melding.MeldingType == "no.ks.fiks.gi.arkivintegrasjon.innsyn.sok.resultat.v1")
            {
                Console.WriteLine("(Svar på " + fileArgs.Melding.SvarPaMelding + ") Melding " + fileArgs.Melding.MeldingId + " " + fileArgs.Melding.MeldingType + " mottas...");

               
                if (fileArgs.Melding.HasPayload)
                { // Verify that message has payload

                    IAsicReader reader = new AsiceReader();
                    using (var inputStream = fileArgs.Melding.DecryptedStream.Result)
                    using (var asice = reader.Read(inputStream))
                    {
                        foreach (var asiceReadEntry in asice.Entries)
                        {
                           
                            using (var entryStream = asiceReadEntry.OpenStream())
                            {
                                StreamReader reader1 = new StreamReader(entryStream);
                                string text = reader1.ReadToEnd();
                                Console.WriteLine(text);
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

                Console.WriteLine("Melding er håndtert i fagsystem ok ......");

                fileArgs.SvarSender.Ack(); // Ack message to remove it from the queue
            }
            else
            {
                Console.WriteLine("Ubehandlet melding i køen " + fileArgs.Melding.MeldingId + " " + fileArgs.Melding.MeldingType);
                //fileArgs.SvarSender.Ack(); // Ack message to remove it from the queue
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Arkivering Service is stopping.2");
            return Task.CompletedTask;
        }
    }

    
}

