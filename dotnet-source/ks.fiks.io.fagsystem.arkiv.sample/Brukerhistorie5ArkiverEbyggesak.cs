using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;
using KS.Fiks.Arkiv.Models.V1.Arkivstruktur;
using KS.Fiks.Arkiv.Models.V1.Innsyn.Sok;
using KS.Fiks.Arkiv.Models.V1.Metadatakatalog;
using KS.Fiks.ASiC_E;
using KS.Fiks.IO.Client;
using KS.Fiks.IO.Client.Configuration;
using KS.Fiks.IO.Client.Models;
using Ks.Fiks.Maskinporten.Client;
using Microsoft.Extensions.Configuration;
using Dokumentbeskrivelse = KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding.Dokumentbeskrivelse;
using Dokumentobjekt = KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding.Dokumentobjekt;
using Journalpost = KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding.Journalpost;
using Korrespondansepart = KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding.Korrespondansepart;
using Part = KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding.Part;
using Registrering = KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding.Registrering;
using Saksmappe = KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding.Saksmappe;

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
            var finnSak = new Sok
            {
                Respons = Respons.Mappe,
                MeldingId = Guid.NewGuid().ToString(),
                System = "eByggesak",
                Tidspunkt = DateTime.Now,
                Skip = 0,
                Take = 2
            };

            finnSak.Parameter.Add(
                    new Parameter
                    {
                        Felt = SokFelt.MappePeriodEksternId,
                        Operator = OperatorType.Equal,
                        Parameterverdier = new Parameterverdier
                        {
                            Stringvalues = {ekstsys, saksid}
                        }
                    });


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
            var gnr = new Klassifikasjon()
            {
                KlasseID = "1234-12/1234",
                Klassifikasjonssystem = "GNR"
            };

            var mn = new Matrikkelnummer
            {
                Gardsnummer = "123",
                Bruksnummer = "456"
            };

            // TODO: Missing fields vs GI 1.1
            var sak = new Saksmappe
            {
                Tittel = "Byggesak 123",
                OffentligTittel = "Byggesak 123",
                AdministrativEnhet = "Byggesaksavdelingen",
                Saksansvarlig = "Byggesaksbehandler",
                Saksdato = new DateTime(),
                Saksstatus = new Saksstatus()
                {
                    KodeProperty= "B"
                },
                Dokumentmedium = new Dokumentmedium()
                {
                    KodeProperty= "elektronisk"
                }, // Code object?
                Journalenhet = "BYG",
                // arkivdel = "BYGG", // Missing and should be a code object
                ReferanseArkivdel = { "BYGG" },  // Should be 0-1, not 0-m as this is the archive part the case belongs to!
                                                              // mappetype = new Kode
                                                              // { kodeKodeProperty= "Saksmappe"}, // Part of simplified message only... Should it be standardized?
                Klassifikasjon = { gnr },
                Part = 
                {
                        new Part
                        {
                            PartNavn = "Fr Tiltakshaver"    // "navn" as for korrespondansepart?
                        }
                },
                Merknad =
                {
                        new Merknad
                        {
                            Merknadstype = new Merknadstype()
                            {
                                KodeProperty= "BYGG"
                            },  // Code object?
                            Merknadstekst = "Saksnummer 20/123 i eByggesak"
                        }
                },
                Matrikkelnummer = {mn},
                // punkt
                // bevaringstid
                // kassasjonsvedtak
                Skjerming = new Skjerming
                {
                    Tilgangsrestriksjon = new Tilgangsrestriksjon()
                    {
                        KodeProperty= "13"
                    }, // Set by server?
                    Skjermingshjemmel = "Ofl § 13, fvl § 123",
                    SkjermingMetadata = { new SkjermingMetadata() {KodeProperty= "tittel" }} // This should be coded
                },
                // prosjekt
                // tilgangsgruppe
                ReferanseEksternNoekkel = new EksternNoekkel
                {
                    Fagsystem = ekstsys,
                    Noekkel = saksid
                }
            };

            var result = SendNySak(sak);

            return "12345"; // Key from archiving of the case / search for the case
        }

        private string nyInngaaendeJournalpost(string ekstsys, string nokkel, string saksid)
        {
            var inn = new Journalpost   // Have diffent objekts for in/out/memo etc. as for simplified?
            {
                // Saksår
                // Sakssekvensnummer
                // referanseForelderMappe = saksid, // Exists in xsd
                Journalposttype = new Journalposttype()
                {
                    KodeProperty= "I"
                },  // Code object?
                Journalstatus = new Journalstatus()
                {
                    KodeProperty= "J"
                },    // Code object?
                DokumentetsDato = new DateTime(),
                Journaldato = new DateTime(),
                Forfallsdato = new DateTime(),
                Korrespondansepart = {
                    new Korrespondansepart
                    {
                        Korrespondanseparttype = new Korrespondanseparttype()
                        {
                            KodeProperty= "avsender"  
                        },    // Code object?
                        Organisasjonid = "123456789",
                        KorrespondansepartNavn = "Testesen",
                        Postadresse = { "c/o Hei og hå", "Testveien 3" },
                        Postnummer = "1234",
                        Poststed = "Poststed",
                    },
                    new Korrespondansepart
                    {
                        Korrespondanseparttype = new Korrespondanseparttype()
                        {
                            KodeProperty= "kopimottager" 
                        },    // Code object?
                        Personid = "12345612345",
                        KorrespondansepartNavn = "Advokat NN",  // How to indicate that a name is a person name if no ID number present?
                        Postadresse = { "Krøsusveien 3" },
                        Postnummer = "2345",
                        Poststed = "Poststedet",
                    },
                    new Korrespondansepart
                    {
                        Saksbehandler = "SBBYGG",
                        AdministrativEnhet = "BYGG"
                    }
                },
                Merknad =
                    {
                        new Merknad
                        {
                            Merknadstype = new Merknadstype()
                            {
                                KodeProperty= "BYGG" 
                            },  // Code object?
                            Merknadstekst = "Journalpostnummer 20/123-5 i eByggesak"
                        }
                    },
                ReferanseEksternNoekkel = new EksternNoekkel
                {
                    Fagsystem = ekstsys,
                    Noekkel = nokkel
                }, 
                Tittel = "Søknad om rammetillatelse 12/123",
                OffentligTittel = "Søknad om rammetillatelse 12/123",
                Skjerming = new Skjerming
                {
                    Tilgangsrestriksjon = new Tilgangsrestriksjon()
                    {
                        KodeProperty= "13" 
                    },
                    Skjermingshjemmel = "Off.loven § 13",
                    Skjermingsvarighet = "60"   // Number of years should be int
                },
                // Dokumenter
                Dokumentbeskrivelse = 
                {
                    new Dokumentbeskrivelse
                    {
                        TilknyttetRegistreringSom = new TilknyttetRegistreringSom()
                        {
                            KodeProperty= "H"
                            
                        },    // Code object?
                        Dokumentnummer = "1",   // Number, should be int
                        Dokumenttype = new Dokumenttype()
                        {
                            KodeProperty= "SØKNAD"
                        },  // Code object?
                        Dokumentstatus = new Dokumentstatus()
                        {
                            KodeProperty= "F"
                        },    // Code object?
                        Tittel = "Søknad om rammetillatelse",
                        Dokumentobjekt =
                        {
                            new Dokumentobjekt
                            {
                                Versjonsnummer = "1",   // Number!
                                Variantformat = new Variantformat()
                                {
                                    KodeProperty= "A"
                                },    // Code object?
                                Format = new Format()
                                {
                                    KodeProperty= "PDF"
                                },     // Arkade wants file type here...
                                MimeType = "application/pdf",
                                ReferanseDokumentfil = "https://ebyggesak.no/hentFil?id=12345&token=67890"
                            }
                        }
                    },
                    new Dokumentbeskrivelse
                    {
                        TilknyttetRegistreringSom = new TilknyttetRegistreringSom()
                        {
                            KodeProperty= "V"
                        },    // Code object?
                        Dokumentnummer = "2",   // Number!
                        Dokumenttype =  new Dokumenttype()
                        {
                            KodeProperty= "KART"
                        },  // Code object?
                        Dokumentstatus = new Dokumentstatus()
                        {
                            KodeProperty= "F"
                        },    // Code object?
                        Tittel = "Situasjonskart",
                        Dokumentobjekt =
                        {
                            new Dokumentobjekt
                            {
                                Versjonsnummer = "1",   // Number!
                                Variantformat = new Variantformat()
                                {
                                    KodeProperty= "A"
                                },    // Number?
                                Format = new Format()
                                {
                                    KodeProperty= "PDF"
                                },     // Arkade wants file type here...
                                MimeType = "application/pdf",
                                ReferanseDokumentfil = "https://ebyggesak.no/hentFil?id=12345&token=67890"
                            }
                        }
                    },
                    new Dokumentbeskrivelse
                    {
                        TilknyttetRegistreringSom = new TilknyttetRegistreringSom()
                        {
                            KodeProperty= "V"
                        },    // Code object?
                        Dokumentnummer = "3",   // NUmber!
                        Dokumenttype = new Dokumenttype()
                        {
                            KodeProperty= "TEGNING"
                        },  // Code object?
                        Dokumentstatus = new Dokumentstatus()
                        {
                            KodeProperty= "F"
                        },    // Code object?
                        Tittel = "Fasade",
                        Dokumentobjekt =
                        {
                            new Dokumentobjekt
                            {
                                Versjonsnummer = "1",   // Number!
                                Variantformat = new Variantformat()
                                {
                                    KodeProperty= "A"
                                },    // Code object?
                                Format = new Format()
                                {
                                    KodeProperty= "PDF"
                                },     // Arkade wants file type here...
                                MimeType = "application/pdf",
                                ReferanseDokumentfil = "https://ebyggesak.no/hentFil?id=12345&token=67890"
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

        private SendtMelding SendSok(Sok _sok)
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

        private SendtMelding SendNySak(Saksmappe saksmappe)
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

            var arkivmld = new Arkivmelding();
            // arkivmld.sluttbrukerIdentifikator = "Fagsystemets brukerid";
            arkivmld.AntallFiler = 0;
            arkivmld.System = saksmappe.ReferanseEksternNoekkel.Fagsystem;
            arkivmld.MeldingId = saksmappe.ReferanseEksternNoekkel.Noekkel;
            arkivmld.Tidspunkt = DateTime.Now;
            arkivmld.Mappe.Add(saksmappe);

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

        private SendtMelding SendNyJournalpost(Registrering jp)
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

            var arkivmld = new Arkivmelding();
            // arkivmld.sluttbrukerIdentifikator = "Fagsystemets brukerid";
            arkivmld.AntallFiler = 0;
            arkivmld.System = jp.ReferanseEksternNoekkel.Fagsystem;
            arkivmld.MeldingId = jp.ReferanseEksternNoekkel.Noekkel;
            arkivmld.Tidspunkt = DateTime.Now;
            arkivmld.Registrering.Add(jp);

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
