using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Schema;
using KS.Fiks.ASiC_E;
using ks.fiks.io.arkivintegrasjon.client.Melding;
using ks.fiks.io.arkivintegrasjon.common.AppSettings;
using ks.fiks.io.arkivintegrasjon.common.FiksIOClient;
using ks.fiks.io.arkivintegrasjon.sample.messages;
using KS.Fiks.IO.Client;
using KS.Fiks.IO.Client.Models;
using ks.fiks.io.fagsystem.arkiv.sample.ForenkletArkivering;
using Microsoft.Extensions.Hosting;
using no.ks.fiks.io.arkivmelding;

namespace ks.fiks.io.arkivsystem.sample
{
    public class ArkivService : IHostedService, IDisposable
    {
        private FiksIOClient client;
        private readonly AppSettings appSettings;

        public ArkivService(AppSettings appSettings)
        {
            this.appSettings = appSettings;
            client = FiksIOClientBuilder.CreateFiksIoClient(appSettings); //CreateFiksIoClient();
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
            var arkivmeldingXmlSchemaSet = new XmlSchemaSet();
            arkivmeldingXmlSchemaSet.Add("http://www.arkivverket.no/standarder/noark5/arkivmelding", Path.Combine("schema", "arkivmelding.xsd"));
            arkivmeldingXmlSchemaSet.Add("http://www.arkivverket.no/standarder/noark5/metadatakatalog", Path.Combine("schema", "metadatakatalog.xsd"));

            var sokXmlSchemaSet = new XmlSchemaSet();
            sokXmlSchemaSet.Add("http://www.arkivverket.no/standarder/noark5/sok", Path.Combine("schema", "sok.xsd"));

            var xmlValidationErrorOccured = false;

            if (ArkivintegrasjonMeldingTypeV1.IsBasis(mottatt.Melding.MeldingType))
            {
                var validationResult = new List<List<string>>();
                var deserializedArkivmelding = new arkivmelding();
                Console.WriteLine($"Melding {mottatt.Melding.MeldingId} {mottatt.Melding.MeldingType} mottas...");

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
                                    Console.WriteLine($"Mottatt vedlegg: {asiceReadEntry.FileName}");

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
                        var ugyldigforespørsel = new Ugyldigforespørsel
                        {
                            ErrorId = Guid.NewGuid().ToString(),
                            Feilmelding = "Feilmelding:\n" + string.Join("\n ", validationResult[0]),
                            ReferanseMeldingId = mottatt.Melding.MeldingId
                        };
                        var errorMessage = mottatt.SvarSender.Svar(MeldingTypeV1.Ugyldigforespørsel, JsonSerializer.Serialize(ugyldigforespørsel), "ugyldigforespørsel.json").Result;
                        Console.WriteLine($"Svarmelding {errorMessage.MeldingId} {errorMessage.MeldingType} sendt");
                        mottatt.SvarSender.Ack(); // Ack message to remove it from the queue
                    }
                    else
                    {
                        var svarmsg = mottatt.SvarSender.Svar(ArkivintegrasjonMeldingTypeV1.Mottatt).Result;
                        Console.WriteLine($"Svarmelding {svarmsg.MeldingId} {svarmsg.MeldingType} sendt...");
                        Console.WriteLine("Melding er mottatt i arkiv ok ......");
                        mottatt.SvarSender.Ack(); // Ack message to remove it from the queue
                    }
                }
                else { // Ugyldig forespørsel
                    var ugyldigforespørsel = new Ugyldigforespørsel
                    {
                        ErrorId = Guid.NewGuid().ToString(),
                        Feilmelding = "Meldingen mangler innhold",
                        ReferanseMeldingId = mottatt.Melding.MeldingId
                    };
                    
                    var svarmsg = mottatt.SvarSender.Svar(MeldingTypeV1.Ugyldigforespørsel, JsonSerializer.Serialize(ugyldigforespørsel), "ugyldigforespørsel.json").Result;
                    Console.WriteLine($"Svarmelding {svarmsg.MeldingId} {svarmsg.MeldingType} sendt");
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

                    var svarmsg2 = mottatt.SvarSender.Svar(ArkivintegrasjonMeldingTypeV1.Kvittering, payload, "arkivmelding.xml").Result;
                    Console.WriteLine("$Svarmelding {svarmsg2.MeldingId} {svarmsg2.MeldingType} sendt...");
                    Console.WriteLine("Arkivering er ok ......");
                }
            }
            else if (ArkivintegrasjonMeldingTypeV1.IsSok(mottatt.Melding.MeldingType))
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
                                {
                                    Console.WriteLine("Mottatt vedlegg: " + asiceReadEntry.FileName);
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
                            ReferanseMeldingId = mottatt.Melding.MeldingId
                        };
                        var errorMessage = mottatt.SvarSender.Svar(MeldingTypeV1.Ugyldigforespørsel, JsonSerializer.Serialize(ugyldigforespørsel), "ugyldigforespørsel.json").Result;
                        Console.WriteLine($"Svarmelding {errorMessage.MeldingId} {errorMessage.MeldingType} sendt");
                        mottatt.SvarSender.Ack(); // Ack message to remove it from the queue
                    }
                }

                //Konverterer til arkivmelding xml
                var simulertSokeresultat = MessageSamples.GetForenkletArkivmeldingInngåendeMedSaksreferanse();
                var arkivmelding = Arkivintegrasjon.ConvertForenkletInnkommendeToArkivmelding(simulertSokeresultat);
                var payload = Arkivintegrasjon.Serialize(arkivmelding);
                //Lager FIKS IO melding
                List<IPayload> payloads = new List<IPayload>();
                payloads.Add(new StringPayload(payload, "arkivmelding.xml"));

                var svarmsg = mottatt.SvarSender.Svar(ArkivintegrasjonMeldingTypeV1.InnsynSokResultat, payloads).Result;
                Console.WriteLine("Svarmelding " + svarmsg.MeldingId + " " + svarmsg.MeldingType + " sendt...");

                Console.WriteLine("Melding er håndtert i arkiv ok ......");

                mottatt.SvarSender.Ack(); // Ack message to remove it from the queue
            }
            else if (ArkivintegrasjonMeldingTypeV1.IsAvansert(mottatt.Melding.MeldingType))
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
    }
}
