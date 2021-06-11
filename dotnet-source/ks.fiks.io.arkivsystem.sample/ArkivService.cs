using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Schema;
using KS.Fiks.ASiC_E;
using ks.fiks.io.arkivintegrasjon.client.ForenkletArkivering;
using ks.fiks.io.arkivintegrasjon.client.Melding;
using ks.fiks.io.arkivintegrasjon.common.AppSettings;
using ks.fiks.io.arkivintegrasjon.sample.messages;
using KS.Fiks.IO.Client;
using KS.Fiks.IO.Client.Configuration;
using KS.Fiks.IO.Client.Models;
using ks.fiks.io.fagsystem.arkiv.sample.ForenkletArkivering;
using Ks.Fiks.Maskinporten.Client;
using Microsoft.Extensions.Hosting;
using no.ks.fiks.io.arkivmelding;
using RabbitMQ.Client;

namespace ks.fiks.io.arkivsystem.sample
{
    public class ArkivService : IHostedService, IDisposable
    {
        private FiksIOClient client;
        private readonly AppSettings appSettings;

        public ArkivService(AppSettings appSettings)
        {
            this.appSettings = appSettings;
            client = CreateFiksIoClient();
        }
        
        public void Dispose()
        {
            client.Dispose();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Arkiv Service is starting.");
            SubscribeToFiksIOClient();
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
                    "no.ks.fiks.gi.arkivintegrasjon.oppdatering.basis.oppdatersaksmappe.v1"

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

            XmlSchemaSet arkivmeldingXmlSchemaSet = new XmlSchemaSet();

            arkivmeldingXmlSchemaSet.Add("http://www.arkivverket.no/standarder/noark5/arkivmelding", "https://raw.githubusercontent.com/ks-no/move-messagetypes/master/arkivmelding/arkivmelding.xsd");
            arkivmeldingXmlSchemaSet.Add("http://www.arkivverket.no/standarder/noark5/metadatakatalog", "https://raw.githubusercontent.com/ks-no/move-messagetypes/master/arkivmelding/metadatakatalog.xsd");

            XmlSchemaSet sokXmlSchemaSet = new XmlSchemaSet();
            sokXmlSchemaSet.Add("http://www.arkivverket.no/standarder/noark5/sok", Path.Combine("schema", "sok.xsd"));

            bool xmlValidationErrorOccured = false;

            if (kjenteMeldingerBasis.Contains(mottatt.Melding.MeldingType))
            {
                var validationResult = new List<List<string>>();
                arkivmelding deserializedArkivmelding = new arkivmelding();
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
                                        StreamReader reader1 = new StreamReader(newEntryStream);
                                        string text = reader1.ReadToEnd();
                                        deserializedArkivmelding = Arkivintegrasjon.DeSerialize(text);
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
                    if (xmlValidationErrorOccured) // Ugyldig forespørsel
                    {
                        var ugyldigforespørsel = new Ugyldigforespørsel()
                        {
                            errorId = Guid.NewGuid().ToString(),
                            feilmelding = "Feilmelding:\n" + string.Join("\n ", validationResult[0])
                        };
                        var errorMessage = mottatt.SvarSender.Svar(MeldingTypeV1.Ugyldigforespørsel, ugyldigforespørsel.ToString(), "ugyldigforespørsel.json").Result;
                        
                        mottatt.SvarSender.Ack(); // Ack message to remove it from the queue
                    }
                    else
                    {
                        var svarmsg = mottatt.SvarSender.Svar(MeldingTypeV1.Mottatt).Result;
                        Console.WriteLine("Svarmelding " + svarmsg.MeldingId + " " + svarmsg.MeldingType + " sendt...");

                        Console.WriteLine("Melding er mottatt i arkiv ok ......");

                        mottatt.SvarSender.Ack(); // Ack message to remove it from the queue
                    }
                }
                else {
                    var svarmsg = mottatt.SvarSender.Svar("no.ks.fiks.gi.arkivintegrasjon.feil.v1", "Meldingen mangler innhold", "feil.txt").Result;
                    Console.WriteLine("Svarmelding " + svarmsg.MeldingId + " " + svarmsg.MeldingType + " Meldingen mangler innhold");

                    mottatt.SvarSender.Ack(); // Ack message to remove it from the queue

                }

                if (!xmlValidationErrorOccured)
                {
                    var kvittering = new arkivmelding();
                    kvittering.tidspunkt = DateTime.Now;
                    var type = deserializedArkivmelding?.Items?[0]?.GetType();

                    if (type == typeof(saksmappe))
                    {
                        var mp = new saksmappe();
                        mp.systemID = new systemID();
                        mp.systemID.Value = Guid.NewGuid().ToString();
                        mp.saksaar = DateTime.Now.Year.ToString();
                        mp.sakssekvensnummer = new Random().Next().ToString();

                        kvittering.Items = new List<saksmappe>() { mp }.ToArray();
                    }
                    else if (type == typeof(journalpost))
                    {
                        var jp = new journalpost();

                        jp.systemID = new systemID();
                        jp.systemID.Value = Guid.NewGuid().ToString();
                        jp.journalaar = DateTime.Now.Year.ToString();
                        jp.journalsekvensnummer = new Random().Next().ToString();
                        jp.journalpostnummer = new Random().Next(1, 100).ToString();

                        kvittering.Items = new List<journalpost>() { jp }.ToArray();
                    }
                    //TODO simulerer at arkivet arkiverer og nøkler skal returneres

                    string payload = Arkivintegrasjon.Serialize(kvittering);

                    var svarmsg2 = mottatt.SvarSender.Svar("no.ks.fiks.gi.arkivintegrasjon.kvittering.v1", payload, "arkivmelding.xml").Result;
                    Console.WriteLine("Svarmelding " + svarmsg2.MeldingId + " " + svarmsg2.MeldingType + " sendt...");

                    Console.WriteLine("Arkivering er ok ......");
                }
            }
            else if (kjenteMeldingerSok.Contains(mottatt.Melding.MeldingType))
            {
                var validationResult = new List<List<string>>();
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
                                        validationResult =  new XmlValidation().ValidateXml(
                                            entryStream,
                                            sokXmlSchemaSet
                                        );
                                        if (validationResult[0].Count > 0)
                                        {
                                            xmlValidationErrorOccured = true;
                                        }
                                        StreamReader reader1 = new StreamReader(entryStream);
                                        string text = reader1.ReadToEnd();
                                        Console.WriteLine("Søker etter: " + text);
                                }
                                else
                                    Console.WriteLine("Mottatt vedlegg: " + asiceReadEntry.FileName);
                            }
                        }
                    }
                    if (xmlValidationErrorOccured)
                    {
                        var errorMessage = mottatt.SvarSender.Svar("no.ks.fiks.gi.arkivintegrasjon.feil.v1", String.Join("\n ", validationResult[0]), "feil.txt").Result;
                        mottatt.SvarSender.Ack(); // Ack message to remove it from the queue
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

        private FiksIOClient CreateFiksIoClient()
        {
            Console.WriteLine("Setter opp FIKS integrasjon for arkivsystem...");
            var accountId = appSettings.FiksIOConfig.FiksIoAccountId;
            var privateKey = File.ReadAllText(appSettings.FiksIOConfig.FiksIoPrivateKey);
            var integrationId = appSettings.FiksIOConfig.FiksIoIntegrationId; 
            var integrationPassword = appSettings.FiksIOConfig.FiksIoIntegrationPassword;
            var scope = appSettings.FiksIOConfig.FiksIoIntegrationScope;
            var audience = appSettings.FiksIOConfig.MaskinPortenAudienceUrl;
            var tokenEndpoint = appSettings.FiksIOConfig.MaskinPortenTokenUrl;
            var issuer = appSettings.FiksIOConfig.MaskinPortenIssuer;
            
            var ignoreSSLError = Environment.GetEnvironmentVariable("AMQP_IGNORE_SSL_ERROR");


            // Fiks IO account configuration
            var account = new KontoConfiguration(
                                accountId,
                                privateKey);

            // Id and password for integration associated to the Fiks IO account.
            var integration = new IntegrasjonConfiguration(
                                    integrationId,
                                    integrationPassword, scope);

            // ID-porten machine to machine configuration
            var maskinporten = new MaskinportenClientConfiguration(
                audience: audience,
                tokenEndpoint: tokenEndpoint,
                issuer: issuer,
                numberOfSecondsLeftBeforeExpire: 10,
                certificate: GetCertificate(appSettings));

            // Optional: Use custom api host (i.e. for connecting to test api)
            var api = new ApiConfiguration(
                scheme: appSettings.FiksIOConfig.ApiScheme,
                host: appSettings.FiksIOConfig.ApiHost,
                port: appSettings.FiksIOConfig.ApiPort);
            
            var sslOption1 = (!string.IsNullOrEmpty(ignoreSSLError) && ignoreSSLError == "true")
                ? new SslOption()
                {
                    Enabled = true,
                    ServerName = appSettings.FiksIOConfig.AmqpHost,
                    CertificateValidationCallback =
                        (RemoteCertificateValidationCallback) ((sender, certificate, chain, errors) => true)
                }
                : null;
                

            // Optional: Use custom amqp host (i.e. for connection to test queue)
            var amqp = new AmqpConfiguration(
                host: appSettings.FiksIOConfig.AmqpHost, //"io.fiks.test.ks.no",
                port: appSettings.FiksIOConfig.AmqpPort,
                sslOption1);

            // Combine all configurations
            var configuration = new FiksIOConfiguration(account, integration, maskinporten, api, amqp);
            return new FiksIOClient(configuration);
        }

        private void SubscribeToFiksIOClient()
        {
            Console.WriteLine("Starter abonnement for FIKS integrasjon for arkivsystem...");
            var accountId = appSettings.FiksIOConfig.FiksIoAccountId; 

            client.NewSubscription(OnReceivedMelding);

            Console.WriteLine("Abonnerer på meldinger på konto " + accountId + " ...");
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
        
        private static X509Certificate2 GetCertificate(AppSettings appSettings)
        {
            if (!string.IsNullOrEmpty(appSettings.FiksIOConfig.MaskinPortenCompanyCertificatePath))
            {
                return new X509Certificate2(File.ReadAllBytes(appSettings.FiksIOConfig.MaskinPortenCompanyCertificatePath), appSettings.FiksIOConfig.MaskinPortenCompanyCertificatePassword);
            }
           
            var store = new X509Store(StoreLocation.CurrentUser);

            store.Open(OpenFlags.ReadOnly);

            var certificates = store.Certificates.Find(X509FindType.FindByThumbprint, appSettings.FiksIOConfig.MaskinPortenCompanyCertificateThumbprint, false);

            store.Close();

            return certificates.Count > 0 ? certificates[0] : null;
        }
    }
}
