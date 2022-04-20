using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Schema;
using KS.Fiks.ASiC_E;
using KS.Fiks.IO.Client.Models;
using Serilog;

namespace ks.fiks.io.arkivsystem.sample.Handlers
{
    public class BaseHandler
    {
        private readonly ILogger Log = Serilog.Log.ForContext(MethodBase.GetCurrentMethod()?.DeclaringType);

        protected string GetPayloadAsString(MottattMeldingArgs mottatt, XmlSchemaSet xmlSchemaSet,
            out bool xmlValidationErrorOccured, out List<List<string>> validationResult)
        {
            xmlValidationErrorOccured = false;

            IAsicReader reader = new AsiceReader();
            using (var inputStream = mottatt.Melding.DecryptedStream.Result)
            using (var asice = reader.Read(inputStream))
            {
                foreach (var asiceReadEntry in asice.Entries)
                {
                    using (var entryStream = asiceReadEntry.OpenStream())
                    {
                        if (asiceReadEntry.FileName.Contains(".xml"))
                        {
                            validationResult = new XmlValidation().ValidateXml(
                                entryStream,
                                xmlSchemaSet
                            );
                            if (validationResult[0].Count > 0)
                            {
                                xmlValidationErrorOccured = true;
                            }

                            var newEntryStream = asiceReadEntry.OpenStream();
                            var reader1 = new StreamReader(newEntryStream);
                            return reader1.ReadToEnd();
                        }
                    }

                    Log.Information("Mottatt vedlegg: {Filename}", asiceReadEntry.FileName);
                }
            }
            validationResult = null;
            return string.Empty;
        }
    }
}