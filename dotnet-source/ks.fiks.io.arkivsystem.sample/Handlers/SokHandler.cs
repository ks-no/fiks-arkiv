using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Schema;
using System.Xml.Serialization;
using KS.Fiks.Arkiv.Models.V1.Innsyn.Sok;
using KS.Fiks.Arkiv.Models.V1.Meldingstyper;
using ks.fiks.io.arkivsystem.sample.Generators;
using ks.fiks.io.arkivsystem.sample.Models;
using KS.Fiks.IO.Client.Models;
using Serilog;

namespace ks.fiks.io.arkivsystem.sample.Handlers
{
    public class SokHandler : BaseHandler
    {
        private static readonly ILogger Log = Serilog.Log.ForContext(MethodBase.GetCurrentMethod()?.DeclaringType);
        
        public SokHandler()
        {
        }

        private Sok GetPayload(MottattMeldingArgs mottatt, XmlSchemaSet xmlSchemaSet,
            out bool xmlValidationErrorOccured, out List<List<string>> validationResult)
        {
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
                return (Sok)new XmlSerializer(typeof(Sok)).Deserialize(textReader);
            }

            xmlValidationErrorOccured = false;
            validationResult = null;
            return null;
        }
        
        public Melding HandleMelding(MottattMeldingArgs mottatt)
        {
            var sok = GetPayload(mottatt, XmlSchemaSet, out var xmlValidationErrorOccured,
                out var validationResult);

            if (xmlValidationErrorOccured)
            {
                return new Melding
                {
                    ResultatMelding = FeilmeldingGenerator.CreateUgyldigforespoerselMelding(validationResult),
                    FileName = "feilmelding.xml",
                    MeldingsType = FiksArkivMeldingtype.Ugyldigforespørsel,
                };
            }

            return SokGenerator.CreateSokResponseMelding(sok);
        }
    }
}