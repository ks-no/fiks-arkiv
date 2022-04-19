using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Schema;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;
using KS.Fiks.ASiC_E;
using ks.fiks.io.arkivintegrasjon.common.Helpers;
using KS.Fiks.IO.Client.Models;
using Serilog;

namespace ks.fiks.io.arkivsystem.sample.Validering
{
    public class Validator
    {
        
        private static readonly ILogger Log = Serilog.Log.ForContext(MethodBase.GetCurrentMethod()?.DeclaringType);

        public static List<List<string>> ValidereXmlMottattMelding(MottattMeldingArgs mottatt, XmlSchemaSet arkivmeldingXmlSchemaSet,
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

     
    }
}