using ks.fiks.io.arkivintegrasjon.sample.messages;
using ks.fiks.io.fagsystem.arkiv.sample.ForenkletArkivering;
using Ks.Fiks.Maskinporten.Client;
using KS.Fiks.ASiC_E;
using KS.Fiks.IO.Client;
using KS.Fiks.IO.Client.Configuration;
using KS.Fiks.IO.Client.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using no.ks.fiks.io.arkivmelding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ks.fiks.io.arkivsystem.sample
{
    public class ArkivService : IHostedService, IDisposable
    {
        FiksIOClient client;
        IConfiguration config;

        public ArkivService()
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
            Console.WriteLine("Arkiv Service is starting.");

            Console.WriteLine("Setter opp FIKS integrasjon for arkivsystem...");
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

            return Task.CompletedTask;
        }

        private void OnReceivedMelding(object sender, MottattMeldingArgs mottatt)
        {
            //Se oversikt over meldingstyper på https://github.com/ks-no/fiks-io-meldingstype-katalog/tree/test/schema

            // Process the message
            List<string> kjenteMeldingerBasis = new List<string>()
                {
                    "no.ks.fiks.gi.arkivintegrasjon.oppdatering.basis.arkivmelding.v1",
                    "no.ks.fiks.gi.arkivintegrasjon.oppdatering.basis.arkivmeldingUtgaaende.v1",
                    "no.ks.fiks.gi.arkivintegrasjon.oppdatering.forenklet.arkivmeldingInnkommende.v1",
                    "no.ks.fiks.gi.arkivintegrasjon.oppdatering.forenklet.oppdatersaksmappe.v1"

                };
            
            List<string> kjenteMeldingerSok = new List<string>()
                {
                    "no.ks.fiks.gi.arkivintegrasjon.innsyn.sok.v1",
                    "no.ks.fiks.gi.arkivintegrasjon.innsyn.sok.v1"
                };

            List<string> kjenteMeldingerFiks = new List<string>()
                {
                    "no.ks.fiks.kvittering.tidsavbrudd",
                    "no.ks.fiks.gi.arkivintegrasjon.feil.v1"
                };

            List<string> kjenteMeldingerAvansert = new List<string>()
                {
                    "no.ks.fiks.gi.arkivintegrasjon.oppdatering.arkivmelding.v1",
                    "no.ks.fiks.gi.arkivintegrasjon.oppdatering.arkivmeldingUtgaaende.v1"
                };

            if (kjenteMeldingerBasis.Contains(mottatt.Melding.MeldingType))
            {
                Console.WriteLine("Melding " + mottatt.Melding.MeldingId + " " + mottatt.Melding.MeldingType + " mottas...");

                //TODO håndtere meldingen med ønsket funksjonalitet
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
                                if (asiceReadEntry.FileName.Contains(".xml")) //TODO regel på navning? alltid arkivmelding.xml?
                                {
                                    StreamReader reader1 = new StreamReader(entryStream);
                                    string text = reader1.ReadToEnd();
                                    Console.WriteLine(text);
                                }
                                else
                                    Console.WriteLine("Mottatt vedlegg: " + asiceReadEntry.FileName);

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

                    //TODO validere arkivmelding og evt sende feil om den ikke er ok for arkivering

                    var svarmsg = mottatt.SvarSender.Svar("no.ks.fiks.gi.arkivintegrasjon.mottatt.v1").Result;
                    Console.WriteLine("Svarmelding " + svarmsg.MeldingId + " " + svarmsg.MeldingType + " sendt...");

                    Console.WriteLine("Melding er mottatt i arkiv ok ......");

                    mottatt.SvarSender.Ack(); // Ack message to remove it from the queue

                }
                else {
                    var svarmsg = mottatt.SvarSender.Svar("no.ks.fiks.gi.arkivintegrasjon.feil.v1", "Meldingen mangler innhold", "feil.txt").Result;
                    Console.WriteLine("Svarmelding " + svarmsg.MeldingId + " " + svarmsg.MeldingType + " Meldingen mangler innhold");

                    mottatt.SvarSender.Ack(); // Ack message to remove it from the queue

                }


                //TODO simulerer at arkivet arkiverer og nøkler skal returneres
                var kvittering = new arkivmelding();
                var sak = new saksmappe();
                sak.saksaar = "2020";
                sak.sakssekvensnummer = "123455676";
                kvittering.Items = new List<saksmappe>() { sak }.ToArray();
                string payload = Arkivintegrasjon.Serialize(kvittering);

                var svarmsg2 = mottatt.SvarSender.Svar("no.ks.fiks.gi.arkivintegrasjon.kvittering.v1", payload, "arkivmelding.xml").Result;
                Console.WriteLine("Svarmelding " + svarmsg2.MeldingId + " " + svarmsg2.MeldingType + " sendt...");

                Console.WriteLine("Arkivering er ok ......");




            }
            else if (kjenteMeldingerSok.Contains(mottatt.Melding.MeldingType))
            {
                Console.WriteLine("Melding " + mottatt.Melding.MeldingId + " " + mottatt.Melding.MeldingType + " mottas...");

                //TODO håndtere meldingen med ønsket funksjonalitet

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
                                if (asiceReadEntry.FileName.Contains(".xml")) //TODO regel på navning? alltid arkivmelding.xml?
                                {
                                    StreamReader reader1 = new StreamReader(entryStream);
                                    string text = reader1.ReadToEnd();
                                    Console.WriteLine("Søker etter: " + text);
                                }
                                else
                                    Console.WriteLine("Mottatt vedlegg: " + asiceReadEntry.FileName);
                            }
                        }
                    }
                }



                //Konverterer til arkivmelding xml
                var simulertSokeresultat = MessageSamples.GetForenkletArkivmeldingInngåendeMedSaksreferanse();
                var arkivmelding = Arkivintegrasjon.ConvertForenkletInnkommendeToArkivmelding(simulertSokeresultat);
                string payload = Arkivintegrasjon.Serialize(arkivmelding);
                //Lager FIKS IO melding
                List<IPayload> payloads = new List<IPayload>();
                payloads.Add(new StringPayload(payload, "arkivmelding.xml"));


                var svarmsg = mottatt.SvarSender.Svar("no.ks.fiks.gi.arkivintegrasjon.innsyn.sok.resultat.v1", payloads).Result;
                Console.WriteLine("Svarmelding " + svarmsg.MeldingId + " " + svarmsg.MeldingType + " sendt...");

                Console.WriteLine("Melding er håndtert i arkiv ok ......");

                mottatt.SvarSender.Ack(); // Ack message to remove it from the queue


            }
            else if (kjenteMeldingerAvansert.Contains(mottatt.Melding.MeldingType))
            {
                Console.WriteLine("Melding " + mottatt.Melding.MeldingId + " " + mottatt.Melding.MeldingType + " mottas...");

                //TODO håndtere meldingen med ønsket funksjonalitet

                Console.WriteLine("Melding er håndtert i arkiv ok ......");

                mottatt.SvarSender.Ack(); // Ack message to remove it from the queue

            }
            else
            {
                Console.WriteLine("Ukjent melding i køen som avvises " + mottatt.Melding.MeldingId + " " + mottatt.Melding.MeldingType);
                mottatt.SvarSender.Nack(); // Nack message to remove it from the queue


            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Arkiv Service is stopping.");

            return Task.CompletedTask;
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
    }
}
