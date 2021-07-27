using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using KS.Fiks.ASiC_E;
using KS.Fiks.IO.Client;
using KS.Fiks.IO.Client.Configuration;
using KS.Fiks.IO.Client.Models;
using Ks.Fiks.Maskinporten.Client;
using Microsoft.Extensions.Configuration;
using no.ks.fiks.io.arkivmelding;
using no.ks.fiks.io.arkivmelding.sok;

namespace ks.fiks.io.fagsystem.arkiv.sample
{
    class Brukerhistorie5ArkiverEbyggesak
    {
        FiksIOClient client;
        IConfiguration config;

        public Brukerhistorie5ArkiverEbyggesak()
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
            Console.WriteLine("Fagsystem Service is starting.");

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

            // TODO: Loop through cases to be transferred from Elements to archive
            // P.t. dummy saksid
            arkiverEbyggesak("123");

            // TODO: Loop through journal posts to be transferred from the archive to Elements

 
            return Task.CompletedTask;
        }
        
        public void arkiverEbyggesak(string saksid)
        {
            // Name of system (eksternsystem)
            var ekstsys = "eByggesak";

            // Existing case in the archive?
            var finnSak = new no.ks.fiks.io.arkivmelding.sok.sok
            {
                respons = respons_type.mappe,
                meldingId = Guid.NewGuid().ToString(),
                system = "eByggesak",
                tidspunkt = DateTime.Now,
                skip = 0,
                take = 2
            };

            var paramlist = new List<parameter>
                {
                    new parameter
                    {
                        felt = field_type.mappeeksternId,
                        @operator = operator_type.equal,
                        parameterverdier = new parameterverdier
                        {
                            Item = new stringvalues {value = new[] {ekstsys, saksid}}
                        }
                    }
                };


            finnSak.parameter = paramlist.ToArray();

            // TODO: Ensure search result as output from SendSok
            var payload = SendSok(finnSak);

            // TODO: Check if there was a case, requires received search result
            string systemid = null;

            // No case found, create one
            if (systemid == null)
            {
                systemid = nySak(ekstsys, saksid);
            }

            // Copy new journal posts
            // TODO Loop through search result from Elements

            // Found incomming as document 1 in the case, dummy ID set
            string jpsystemid = nyInngaaendeJournalpost(ekstsys, saksid + "-1", systemid);

            // Repeat for outgoing

            // Repeat for memos

            // Repeat for case presentations / proposals (political meetings)
        }

        private string nySak(string ekstsys, string saksid)
        {
            klasse gnr = new klasse
            {
                klasseID = "1234-12/1234",
                klassifikasjonssystem = "GNR"
            };

            Matrikkelnummer mn = new Matrikkelnummer
            {
                gaardsnummer = "123",
                bruksnummer = "456"
            };

            // TODO: Missing fields vs GI 1.1
            saksmappe sak = new saksmappe
            {
                tittel = "Byggesak 123",
                offentligTittel = "Byggesak 123",
                administrativEnhet = "Byggesaksavdelingen",
                saksansvarlig = "Byggesaksbehandler",
                saksdato = new DateTime(),
                saksstatus = "B",
                dokumentmedium = "elektronisk", // Code object?
                journalenhet = "BYG",
                // arkivdel = "BYGG", // Missing and should be a code object
                referanseArkivdel = new string[] { "BYGG" },  // Should be 0-1, not 0-m as this is the archive part the case belongs to!
                                                              // mappetype = new Kode
                                                              // { kodeverdi = "Saksmappe"}, // Part of simplified message only... Should it be standardized?
                klasse = new klasse[] { gnr },
                part = new part[]
                {
                        new part
                        {
                            partNavn = "Fr Tiltakshaver"    // "navn" as for korrespondansepart?
                        }
                },
                merknad = new merknad[]
                {
                        new merknad
                        {
                            merknadstype = "BYGG",  // Code object?
                            merknadstekst = "Saksnummer 20/123 i eByggesak"
                        }
                },
                matrikkelnummer = new Matrikkelnummer[] {mn},
                // punkt
                // bevaringstid
                // kassasjonsvedtak
                skjerming = new skjerming
                {
                    tilgangsrestriksjon = "13", // Set by server?
                    skjermingshjemmel = "Ofl § 13, fvl § 123",
                    skjermingMetadata = new string[] { "tittel" } // This should be coded
                },
                // prosjekt
                // tilgangsgruppe
                referanseEksternNoekkel = new eksternNoekkel
                {
                    fagsystem = ekstsys,
                    noekkel = saksid
                }
            };

            var result = SendNySak(sak);

            return "12345"; // Key from archiving of the case / search for the case
        }

        private string nyInngaaendeJournalpost(string ekstsys, string nokkel, string saksid)
        {
            journalpost inn = new journalpost   // Have diffent objekts for in/out/memo etc. as for simplified?
            {
                // Saksår
                // Sakssekvensnummer
                // referanseForelderMappe = saksid, // Exists in xsd
                journalposttype = "I",  // Code object?
                journalstatus = "J",    // Code object?
                dokumentetsDato = new DateTime(),
                journaldato = new DateTime(),
                forfallsdato = new DateTime(),
                korrespondansepart = new korrespondansepart[] {
                    new korrespondansepart
                    {
                        korrespondanseparttype = "avsender",    // Code object?
                        Item = new EnhetsidentifikatorType      // Field name should indicate that this is an ID
                        {
                            organisasjonsnummer = "123456789"
                        },
                        korrespondansepartNavn = "Testesen",
                        postadresse = new string[] { "c/o Hei og hå", "Testveien 3" },
                        postnummer = "1234",
                        poststed = "Poststed",
                    },
                    new korrespondansepart
                    {
                        korrespondanseparttype = "kopimottager",    // Code object?
                        Item = new FoedselsnummerType
                        {
                            foedselsnummer = "12345612345"
                        },
                        korrespondansepartNavn = "Advokat NN",  // How to indicate that a name is a person name if no ID number present?
                        postadresse = new string[] { "Krøsusveien 3" },
                        postnummer = "2345",
                        poststed = "Poststedet",
                    },
                    new korrespondansepart
                    {
                        saksbehandler = "SBBYGG",
                        administrativEnhet = "BYGG"
                    }
                },
                merknad = new merknad[]
                    {
                        new merknad
                        {
                            merknadstype = "BYGG",  // Code object?
                            merknadstekst = "Journalpostnummer 20/123-5 i eByggesak"
                        }
                    },
                referanseEksternNoekkel = new eksternNoekkel
                {
                    fagsystem = ekstsys,
                    noekkel = nokkel
                },
                tittel = "Søknad om rammetillatelse 12/123",
                offentligTittel = "Søknad om rammetillatelse 12/123",
                skjerming = new skjerming
                {
                    tilgangsrestriksjon = "13",
                    skjermingshjemmel = "Off.loven § 13",
                    skjermingsvarighet = "60"   // Number of years should be int
                },
                // Dokumenter
                dokumentbeskrivelse = new dokumentbeskrivelse[]
                {
                    new dokumentbeskrivelse
                    {
                        tilknyttetRegistreringSom = "H",    // Code object?
                        dokumentnummer = "1",   // Number, should be int
                        dokumenttype = "SØKNAD",  // Code object?
                        dokumentstatus = "F",    // Code object?
                        tittel = "Søknad om rammetillatelse",
                        dokumentobjekt = new dokumentobjekt[]
                        {
                            new dokumentobjekt
                            {
                                versjonsnummer = "1",   // Number!
                                variantformat = "A",    // Code object?
                                format = "PDF",     // Arkade wants file type here...
                                mimeType = "application/pdf",
                                referanseDokumentfil = "https://ebyggesak.no/hentFil?id=12345&token=67890"
                            }
                        }
                    },
                    new dokumentbeskrivelse
                    {
                        tilknyttetRegistreringSom = "V",    // Code object?
                        dokumentnummer = "2",   // Number!
                        dokumenttype = "KART",  // Code object?
                        dokumentstatus = "F",    // Code object?
                        tittel = "Situasjonskart",
                        dokumentobjekt = new dokumentobjekt[]
                        {
                            new dokumentobjekt
                            {
                                versjonsnummer = "1",   // Number!
                                variantformat = "A",    // Number?
                                format = "PDF",     // Arkade wants file type here...
                                mimeType = "application/pdf",
                                referanseDokumentfil = "https://ebyggesak.no/hentFil?id=12345&token=67890"
                            }
                        }
                    },
                    new dokumentbeskrivelse
                    {
                        tilknyttetRegistreringSom = "V",    // Code object?
                        dokumentnummer = "3",   // NUmber!
                        dokumenttype = "TEGNING",  // Code object?
                        dokumentstatus = "F",    // Code object?
                        tittel = "Fasade",
                        dokumentobjekt = new dokumentobjekt[]
                        {
                            new dokumentobjekt
                            {
                                versjonsnummer = "1",   // Number!
                                variantformat = "A",    // Code object?
                                format = "PDF",     // Arkade wants file type here...
                                mimeType = "application/pdf",
                                referanseDokumentfil = "https://ebyggesak.no/hentFil?id=12345&token=67890"
                            }
                        }
                    }
                }
            };

            var result = SendNyJournalpost(inn);

            return saksid + "-1"; // Key from archiving of journal post / search for journal post
        }

        private static X509Certificate2 GetCertificate(string ThumbprintIdPortenVirksomhetssertifikat)
        {

            // Det samme virksomhetssertifikat som er registrert hos ID-porten
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

        private SendtMelding SendSok(sok _sok)
        {
            Guid receiverId = Guid.Parse(config["sendToAccountId"]); // Receiver id as Guid
            Guid senderId = Guid.Parse(config["accountId"]); // Sender id as Guid

            var konto = client.Lookup(new LookupRequest("KOMM:0825", "no.ks.fiks.gi.arkivintegrasjon.innsyn.v1", 3)); //TODO for å finne receiverId
            //Prosess også?

            var messageRequest = new MeldingRequest(
                                      mottakerKontoId: receiverId,
                                      avsenderKontoId: senderId,
                                      meldingType: "no.ks.fiks.gi.arkivintegrasjon.innsyn.sok.v1"); // Message type as string
                                                                                                    //Se oversikt over meldingstyper på https://github.com/ks-no/fiks-io-meldingstype-katalog/tree/test/schema

            // Converts to arkivmelding xml
            string payload = Serialize(_sok);
            // Creates FIKS IO message
            List<IPayload> payloads = new List<IPayload>();
            payloads.Add(new StringPayload(payload, "sok.xml"));

            // Sends to FIKS IO (archive solution)
            SendtMelding msg = client.Send(messageRequest, payloads).Result;
            Console.WriteLine("Message sok " + msg.MeldingId.ToString() + " sent..." + msg.MeldingType);
            Console.WriteLine(payload);

            // TODO: Catch OnReceivedMelding so the search result may be found

            return msg;
        }

        private SendtMelding SendNySak(saksmappe sak)
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

            var arkivmld = new arkivmelding();
            // arkivmld.sluttbrukerIdentifikator = "Fagsystemets brukerid";
            arkivmld.antallFiler = 0;
            arkivmld.system = sak.referanseEksternNoekkel.fagsystem;
            arkivmld.meldingId = sak.referanseEksternNoekkel.noekkel;
            arkivmld.tidspunkt = DateTime.Now;
            arkivmld.Items = new saksmappe[] { sak };

            string payload = Serialize(arkivmld);

            // Creates FIKS IO message
            List<IPayload> payloads = new List<IPayload>();
            payloads.Add(new StringPayload(payload, "sak.xml"));

            // Sends to FIKS IO (arkive solution)
            var msg = client.Send(messageRequest, payloads).Result;
            Console.WriteLine("Melding " + msg.MeldingId.ToString() + " sendt..." + msg.MeldingType);
            Console.WriteLine(payload);

            // TODO: Await result from archive and return key
            return null;
        }

        private SendtMelding SendNyJournalpost(journalpost jp)
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

            var arkivmld = new arkivmelding();
            // arkivmld.sluttbrukerIdentifikator = "Fagsystemets brukerid";
            arkivmld.antallFiler = 0;
            arkivmld.system = jp.referanseEksternNoekkel.fagsystem;
            arkivmld.meldingId = jp.referanseEksternNoekkel.noekkel;
            arkivmld.tidspunkt = DateTime.Now;
            arkivmld.Items = new journalpost[] { jp };

            string payload = Serialize(arkivmld);

            // Creates FIKS IO message
            List<IPayload> payloads = new List<IPayload>();
            payloads.Add(new StringPayload(payload, "journalpost.xml"));

            // Sends to FIKS IO (archive solution)
            var msg = client.Send(messageRequest, payloads).Result;
            Console.WriteLine("Melding " + msg.MeldingId.ToString() + " sendt..." + msg.MeldingType);
            Console.WriteLine(payload);

            // TODO: Await result from archive and return key
            return null;
        }

        public static string Serialize(object arkivmelding)
        {
            var serializer = new XmlSerializer(arkivmelding.GetType());
            var stringWriter = new StringWriter();
            serializer.Serialize(stringWriter, arkivmelding);
            return stringWriter.ToString();
        }

    }
}
