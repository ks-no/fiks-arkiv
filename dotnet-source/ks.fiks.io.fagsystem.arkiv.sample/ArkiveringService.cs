using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FIKS.eMeldingArkiv.eMeldingForenkletArkiv;
using ks.fiks.io.fagsystem.arkiv.sample.ForenkletArkivering;
using Ks.Fiks.Maskinporten.Client;
using KS.Fiks.IO.Client;
using KS.Fiks.IO.Client.Configuration;
using KS.Fiks.IO.Client.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ks.fiks.io.fagsystem.arkiv.sample
{
    public class ArkiveringService : IHostedService, IDisposable
    {
        FiksIOClient client;
        IConfiguration config;

        public ArkiveringService()
        {
            config = new ConfigurationBuilder()
               .AddJsonFile("appsettings.json", true, true)
               .AddJsonFile("appsettings.development.json", true, true)
               .Build();
        }
        public void Dispose()
        {
            client.Dispose();
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Arkivering Service is starting.");

            Console.WriteLine("Setter opp FIKS integrasjon for fagsystem...");
            Guid accountId = Guid.Parse(config["accountId"]);  /* Fiks IO accountId as Guid Banke kommune eByggesak konto*/
            string privateKey = File.ReadAllText("privkey.pem"); ; /* Private key for offentlig nøkkel supplied to Fiks IO account */
            Guid integrationId = Guid.Parse(config["integrationId"]); /* Integration id as Guid eByggesak system X */
            string integrationPassword = config["integrationPassword"];  /* Integration password */

            // Fiks IO account configuration
            var account = new KontoConfiguration(
                                accountId,
                                privateKey);

            // Id and password for integration associated to the Fiks IO account.
            var integration = new IntegrasjonConfiguration(
                                    integrationId,
                                    integrationPassword, "ks:fiks");

            // ID-porten machine to machine configuration
            var maskinporten = new MaskinportenClientConfiguration(
                audience: @"https://oidc-ver2.difi.no/idporten-oidc-provider/", // ID-porten audience path
                tokenEndpoint: @"https://oidc-ver2.difi.no/idporten-oidc-provider/token", // ID-porten token path
                issuer: @"arkitektum_test",  // issuer name
                numberOfSecondsLeftBeforeExpire: 10, // The token will be refreshed 10 seconds before it expires
                certificate: GetCertificate(config["ThumbprintIdPortenVirksomhetssertifikat"]));

            // Optional: Use custom api host (i.e. for connecting to test api)
            var api = new ApiConfiguration(
                            scheme: "https",
                            host: "api.fiks.test.ks.no",
                            port: 443);

            // Optional: Use custom amqp host (i.e. for connection to test queue)
            var amqp = new AmqpConfiguration(
                            host: "io.fiks.test.ks.no",
                            port: 5671);

            // Combine all configurations
            var configuration = new FiksIOConfiguration(account, integration, maskinporten, api, amqp);
            client = new FiksIOClient(configuration); // See setup of configuration below



            client.NewSubscription(OnReceivedMelding);

            Console.WriteLine("Abonnerer på meldinger på konto " + accountId.ToString() + " ...");


            SendInngående();

            SendUtgående();

           


            return Task.CompletedTask;
        }

        private void SendInngående()
        {
            Guid receiverId = Guid.Parse(config["sendToAccountId"]); // Receiver id as Guid
            Guid senderId = Guid.Parse(config["accountId"]); // Sender id as Guid

            var konto = client.Lookup(new LookupRequest("KOMM:0825", "no.geointegrasjon.arkiv.oppdatering.arkivmeldingforenklet.v1", 3)); //TODO for å finne receiverId
            //Prosess også?

            var messageRequest = new MeldingRequest(
                                      mottakerKontoId: receiverId,
                                      avsenderKontoId: senderId,
                                      meldingType: "no.geointegrasjon.arkiv.oppdatering.arkivmeldingforenkletInnkommende.v1"); // Message type as string
                                                                                                                              //Se oversikt over meldingstyper på https://github.com/ks-no/fiks-io-meldingstype-katalog/tree/test/schema


            //Fagsystem definerer ønsket struktur
            ArkivmeldingForenkletInnkommende inng = new ArkivmeldingForenkletInnkommende();
            inng.sluttbrukerIdentifikator = "Fagsystemets brukerid";

            inng.nyInnkommendeJournalpost = new InnkommendeJournalpost
            {
                tittel = "Bestilling av oppmålingsforretning ...",
                mottattDato = DateTime.Today,
                dokumentetsDato = DateTime.Today.AddDays(-2),
                offentlighetsvurdertDato = DateTime.Today,
            };

            inng.nyInnkommendeJournalpost.referanseEksternNøkkel = new EksternNøkkel
            {
                fagsystem = "Fagsystem X",
                nøkkel = "e4712424-883c-4068-9cb7-97ac679d7232"
            };
            
            inng.nyInnkommendeJournalpost.internMottaker = new List<KorrespondansepartIntern>
            {
                new KorrespondansepartIntern() { 
                    administrativEnhet = "Oppmålingsetaten",
                    referanseAdministrativEnhet = "b631f24b-48fb-4b5c-838e-6a1f7d56fae2"
                }
            };

            inng.nyInnkommendeJournalpost.mottaker = new List<Korrespondansepart>
            {
                new Korrespondansepart() { 
                    navn = "Test kommune", 
                    enhetsidentifikator = new Enhetsidentifikator() { 
                        organisasjonsnummer = "123456789" 
                    }, 
                    postadresse = new EnkelAdresse() { 
                        adresselinje1 = "Oppmålingsetaten", 
                        adresselinje2 = "Rådhusgate 1", 
                        postnr = "3801", 
                        poststed = "Bø" 
                    } 
                }
            };


            inng.nyInnkommendeJournalpost.avsender = new List<Korrespondansepart>
            {
                new Korrespondansepart() { 
                    navn = "Anita Avsender",
                    personid = new Personidentifikator() { personidentifikatorType = "F",  personidentifikatorNr = "12345678901"},
                    postadresse = new EnkelAdresse() { 
                        adresselinje1 = "Gate 1", 
                        postnr = "3801", 
                        poststed = "Bø" } 
                }
            };


            inng.nyInnkommendeJournalpost.hoveddokument = new ForenkletDokument
            {
                tittel = "Rekvisisjon av oppmålingsforretning",
                filnavn = "rekvisisjon.pdf"
            };

            inng.nyInnkommendeJournalpost.vedlegg = new List<ForenkletDokument>
            {
                new ForenkletDokument(){
                    tittel = "Vedlegg 1",
                    filnavn = "vedlegg.pdf"
                }
            };

            //osv...

            //Konverterer til arkivmelding xml
            var arkivmelding = Arkivintegrasjon.ConvertForenkletInnkommendeToArkivmelding(inng);
            string payload = Arkivintegrasjon.Serialize(arkivmelding);

            //Lager FIKS IO melding
            List<IPayload> payloads = new List<IPayload>();
            payloads.Add(new StringPayload(payload, "innkommendejournalpost.xml"));
            payloads.Add(new FilePayload(@"samples\rekvisisjon.pdf"));
            payloads.Add(new FilePayload(@"samples\vedlegg.pdf"));

            //Sender til FIKS IO (arkiv løsning)
            var msg = client.Send(messageRequest, payloads).Result;
            Console.WriteLine("Melding " + msg.MeldingId.ToString() + " sendt..." + msg.MeldingType + "...med 1 vedlegg");
            Console.WriteLine(payload);

        }

        private void SendInngåendeBrukerhistorie3_1()
        {
            Guid receiverId = Guid.Parse(config["sendToAccountId"]); // Receiver id as Guid
            Guid senderId = Guid.Parse(config["accountId"]); // Sender id as Guid

            var konto = client.Lookup(new LookupRequest("KOMM:0825", "no.geointegrasjon.arkiv.oppdatering.arkivmeldingforenklet.v1", 3)); //TODO for å finne receiverId
            //Prosess også?

            var messageRequest = new MeldingRequest(
                                      mottakerKontoId: receiverId,
                                      avsenderKontoId: senderId,
                                      meldingType: "no.geointegrasjon.arkiv.oppdatering.arkivmeldingforenkletInnkommende.v1"); // Message type as string
                                                                                                                               //Se oversikt over meldingstyper på https://github.com/ks-no/fiks-io-meldingstype-katalog/tree/test/schema


            //Fagsystem definerer ønsket struktur
            ArkivmeldingForenkletInnkommende inng = new ArkivmeldingForenkletInnkommende();
            inng.sluttbrukerIdentifikator = "9hs2ir";

            inng.nyInnkommendeJournalpost = new InnkommendeJournalpost
            {
                tittel = "Startlån søknad(Ref=e4reke, SakId=e4reke)",
                mottattDato = DateTime.Today,
                dokumentetsDato = DateTime.Today.AddDays(-2),
                offentlighetsvurdertDato = DateTime.Today
            };

            inng.nyInnkommendeJournalpost.referanseEksternNøkkel = new EksternNøkkel
            {
                fagsystem = "Fagsystem X",
                nøkkel = "e4reke"
            };

           
            inng.nyInnkommendeJournalpost.mottaker = new List<Korrespondansepart>
            {
                new Korrespondansepart() {
                    navn = "Test kommune",
                    enhetsidentifikator = new Enhetsidentifikator() {
                        organisasjonsnummer = "123456789"
                    },
                    postadresse = new EnkelAdresse() {
                        adresselinje1 = "Startlån avd",
                        adresselinje2 = "Rådhusgate 1",
                        postnr = "3801",
                        poststed = "Bø"
                    }
                }
            };


            inng.nyInnkommendeJournalpost.avsender = new List<Korrespondansepart>
            {
                new Korrespondansepart() {
                    navn = "Anita Søker",
                    personid = new Personidentifikator() { personidentifikatorType = "F",  personidentifikatorNr = "12345678901"},
                    postadresse = new EnkelAdresse() {
                        adresselinje1 = "Gate 1",
                        postnr = "3801",
                        poststed = "Bø" }
                }
            };


            inng.nyInnkommendeJournalpost.hoveddokument = new ForenkletDokument
            {
                tittel = "Søknad om startlån",
                filnavn = "søknad.pdf"
            };

            inng.nyInnkommendeJournalpost.vedlegg = new List<ForenkletDokument>
            {
                new ForenkletDokument(){
                    tittel = "Vedlegg 1",
                    filnavn = "vedlegg.pdf"
                }
            };

            

            //Konverterer til arkivmelding xml
            var arkivmelding = Arkivintegrasjon.ConvertForenkletInnkommendeToArkivmelding(inng);
            string payload = Arkivintegrasjon.Serialize(arkivmelding);

            //Lager FIKS IO melding
            List<IPayload> payloads = new List<IPayload>();
            payloads.Add(new StringPayload(payload, "innkommendejournalpost.xml"));
            payloads.Add(new FilePayload(@"samples\søknad.pdf"));
            payloads.Add(new FilePayload(@"samples\vedlegg.pdf"));

            //Sender til FIKS IO (arkiv løsning)
            var msg = client.Send(messageRequest, payloads).Result;
            Console.WriteLine("Melding " + msg.MeldingId.ToString() + " sendt..." + msg.MeldingType + "...med 1 vedlegg");
            Console.WriteLine(payload);

        }

        private void SendUtgående()
        {
            Guid receiverId = Guid.Parse(config["sendToAccountId"]); // Receiver id as Guid
            Guid senderId = Guid.Parse(config["accountId"]); // Sender id as Guid

            var konto = client.Lookup(new LookupRequest("KOMM:0825", "no.geointegrasjon.arkiv.oppdatering.arkivmeldingforenklet.v1", 3)); //TODO for å finne receiverId
            //Prosess også?

            var messageRequest = new MeldingRequest(
                                      mottakerKontoId: receiverId,
                                      avsenderKontoId: senderId,
                                      meldingType: "no.geointegrasjon.arkiv.oppdatering.arkivmeldingforenkletUtgaaende.v1"); // Message type as string
                                                                                                                             //Se oversikt over meldingstyper på https://github.com/ks-no/fiks-io-meldingstype-katalog/tree/test/schema


            //Fagsystem definerer ønsket struktur
            ArkivmeldingForenkletUtgaaende utg = new ArkivmeldingForenkletUtgaaende
            {
                sluttbrukerIdentifikator = "Fagsystemets brukerid",
                nyUtgaaendeJournalpost = new UtgaaendeJournalpost()
            };

            utg.nyUtgaaendeJournalpost.tittel = "Tillatelse til ...";
            utg.nyUtgaaendeJournalpost.referanseEksternNøkkel = new EksternNøkkel
            {
                fagsystem = "Fagsystem X",
                nøkkel = "759d7aab-6f41-487d-bdb9-dd177ee887c1"
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
                    filnavn = "vedlegg.pdf"
                }
            };

            //osv...

            //Konverterer til arkivmelding xml
            var arkivmelding = Arkivintegrasjon.ConvertForenkletUtgaaendeToArkivmelding(utg);
            string payload = Arkivintegrasjon.Serialize(arkivmelding);

            //Lager FIKS IO melding
            List<IPayload> payloads = new List<IPayload>();
            payloads.Add(new StringPayload(payload, "utgaaendejournalpost.xml"));
            payloads.Add(new FilePayload(@"samples\vedtak.pdf"));
            payloads.Add(new FilePayload(@"samples\vedlegg.pdf"));

            //Sender til FIKS IO (arkiv løsning)
            var msg = client.Send(messageRequest, payloads).Result;
            Console.WriteLine("Melding " + msg.MeldingId.ToString() + " sendt..." + msg.MeldingType + "...med 1 vedlegg");
            Console.WriteLine(payload);

        }

        private void SendUtgåendeUtvidet()
        {
            Guid receiverId = Guid.Parse(config["sendToAccountId"]); // Receiver id as Guid
            Guid senderId = Guid.Parse(config["accountId"]); // Sender id as Guid

            var konto = client.Lookup(new LookupRequest("KOMM:0825", "no.geointegrasjon.arkiv.oppdatering.arkivmelding.v1", 3)); //TODO for å finne receiverId
            //Prosess også?

            var messageRequest = new MeldingRequest(
                                      mottakerKontoId: receiverId,
                                      avsenderKontoId: senderId,
                                      meldingType: "no.geointegrasjon.arkiv.oppdatering.arkivmeldingUtgaaende.v1"); // Message type as string
                                                                                                                             //Se oversikt over meldingstyper på https://github.com/ks-no/fiks-io-meldingstype-katalog/tree/test/schema


            //Fagsystem definerer ønsket struktur
            ArkivmeldingForenkletUtgaaende utg = new ArkivmeldingForenkletUtgaaende();
            utg.sluttbrukerIdentifikator = "Fagsystemets brukerid";
            utg.nyUtgaaendeJournalpost = new UtgaaendeJournalpost();
            utg.nyUtgaaendeJournalpost.referanseEksternNøkkel = new EksternNøkkel();
            utg.nyUtgaaendeJournalpost.referanseEksternNøkkel.fagsystem = "Fagsystem X";
            utg.nyUtgaaendeJournalpost.referanseEksternNøkkel.nøkkel = Guid.NewGuid().ToString();

            utg.nyUtgaaendeJournalpost.tittel = "Tillatelse til ...";

            utg.nyUtgaaendeJournalpost.internAvsender = new List<KorrespondansepartIntern>();
            utg.nyUtgaaendeJournalpost.internAvsender.Add(new KorrespondansepartIntern() { saksbehandler = "Sigve Saksbehandler" });

            utg.nyUtgaaendeJournalpost.mottaker = new List<Korrespondansepart>();
            utg.nyUtgaaendeJournalpost.mottaker.Add(new Korrespondansepart() { navn = "Mons Mottaker", postadresse = new EnkelAdresse() { adresselinje1 = "Gate 1", postnr = "3801", poststed = "Bø" } });
            utg.nyUtgaaendeJournalpost.mottaker.Add(new Korrespondansepart() { navn = "Foretak Mottaker", postadresse = new EnkelAdresse() { adresselinje1 = "Forretningsgate 1", postnr = "3801", poststed = "Bø" } });

            utg.nyUtgaaendeJournalpost.hoveddokument = new ForenkletDokument();
            utg.nyUtgaaendeJournalpost.hoveddokument.tittel = "Vedtak om tillatelse til ...";
            utg.nyUtgaaendeJournalpost.hoveddokument.filnavn = "vedtak.pdf";

            utg.nyUtgaaendeJournalpost.vedlegg = new List<ForenkletDokument>();
            var vedlegg1 = new ForenkletDokument();
            vedlegg1.tittel = "Vedlegg 1";
            vedlegg1.filnavn = "vedlegg.pdf";
            utg.nyUtgaaendeJournalpost.vedlegg.Add(vedlegg1);

            //osv...

            //Konverterer til arkivmelding xml
            var arkivmelding = Arkivintegrasjon.ConvertForenkletUtgaaendeToArkivmelding(utg);

            //TODO redigere arkivmelding



            string payload = Arkivintegrasjon.Serialize(arkivmelding);

            //Lager FIKS IO melding
            List<IPayload> payloads = new List<IPayload>();
            payloads.Add(new StringPayload(payload, "utgaaendejournalpost.xml"));
            payloads.Add(new FilePayload(@"samples\vedtak.pdf"));
            payloads.Add(new FilePayload(@"samples\vedlegg.pdf"));

            //Sender til FIKS IO (arkiv løsning)
            var msg = client.Send(messageRequest, payloads).Result;
            Console.WriteLine("Melding " + msg.MeldingId.ToString() + " sendt..." + msg.MeldingType + "...med 1 vedlegg");
            Console.WriteLine(payload);

        }
        private static X509Certificate2 GetCertificate(string ThumbprintIdPortenVirksomhetssertifikat)
        {

            //Det samme virksomhetssertifikat som er registrert hos ID-porten
            X509Store store = new X509Store(StoreLocation.CurrentUser);
            X509Certificate2 cer = null;
            store.Open(OpenFlags.ReadOnly);
            //Henter Arkitektum sitt virksomhetssertifikat
            X509Certificate2Collection cers = store.Certificates.Find(X509FindType.FindByThumbprint, ThumbprintIdPortenVirksomhetssertifikat, false);
            if (cers.Count > 0)
            {
                cer = cers[0];
            };
            store.Close();

            return cer;
        }

        static void OnReceivedMelding(object sender, MottattMeldingArgs fileArgs)
        {
            //Se oversikt over meldingstyper på https://github.com/ks-no/fiks-io-meldingstype-katalog/tree/test/schema

            // Process the message


            if (fileArgs.Melding.MeldingType == "no.ks.geointegrasjon.ok.v1")
            {
                Console.WriteLine("Melding " + fileArgs.Melding.MeldingId + " " + fileArgs.Melding.MeldingType + " mottas...");

                //TODO håndtere meldingen med ønsket funksjonalitet

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

