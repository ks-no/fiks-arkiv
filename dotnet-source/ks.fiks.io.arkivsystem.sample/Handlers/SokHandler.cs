using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Schema;
using System.Xml.Serialization;
using KS.Fiks.IO.Arkiv.Client.Models.Innsyn.Sok;
using KS.Fiks.IO.Client.Models;
using Serilog;

namespace ks.fiks.io.arkivsystem.sample.Handlers
{
    public class SokHandler : BaseHandler
    {
        private static readonly ILogger Log = Serilog.Log.ForContext(MethodBase.GetCurrentMethod()?.DeclaringType);

        public static Sok GetPayload(MottattMeldingArgs mottatt, XmlSchemaSet xmlSchemaSet,
            out bool xmlValidationErrorOccured, out List<List<string>> validationResult)
        {
            Sok sok = null;
            if (mottatt.Melding.HasPayload)
            {
                // Verify that message has payload
                var text = GetPayloadAsString(mottatt, xmlSchemaSet, out xmlValidationErrorOccured, out validationResult);
                Log.Information("Parsing sok: {SokText}", text);
                if (string.IsNullOrEmpty(text))
                {
                    Log.Error("Tom sok? Text: {Sok}", text);
                }

                using var textReader = (TextReader)new StringReader(text);
                sok = (Sok)new XmlSerializer(typeof(Sok)).Deserialize(textReader);
            }

            xmlValidationErrorOccured = false;
            validationResult = null;
            return sok;
        }
    }
}