using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using KS.Fiks.Arkiv.Models.V1.Innsyn.Sok;
using ks.fiks.io.arkivsystem.sample.Generators;
using ks.fiks.io.arkivsystem.sample.Models;
using KS.Fiks.IO.Client.Models;
using KS.Fiks.IO.Client.Models.Feilmelding;
using Serilog;

namespace ks.fiks.io.arkivsystem.sample.Handlers
{
    public class SokHandler : BaseHandler
    {
        private static readonly ILogger Log = Serilog.Log.ForContext(MethodBase.GetCurrentMethod()?.DeclaringType);
        private readonly XmlSchemaSet sokXmlSchemaSet;
        
        public SokHandler()
        {
            sokXmlSchemaSet = new XmlSchemaSet();
            var arkivModelsAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .SingleOrDefault(assembly => assembly.GetName().Name == "KS.Fiks.Arkiv.Models.V1");
            
            using (var schemaStream = arkivModelsAssembly?.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.sok.xsd"))
            {
                if (schemaStream != null)
                {
                    using var schemaReader = XmlReader.Create(schemaStream);
                    sokXmlSchemaSet.Add("http://www.ks.no/standarder/fiks/arkiv/sok/v1", schemaReader);
                }
            }
            using (var schemaStream = arkivModelsAssembly?.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.arkivstruktur.xsd"))
            {
                if (schemaStream != null)
                {
                    using var schemaReader = XmlReader.Create(schemaStream);
                    sokXmlSchemaSet.Add("http://www.arkivverket.no/standarder/noark5/arkivstruktur",
                        schemaReader);
                }
            }
            
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
            var sok = GetPayload(mottatt, sokXmlSchemaSet, out var xmlValidationErrorOccured,
                out var validationResult);

            if (xmlValidationErrorOccured)
            {
                return new Melding
                {
                    ResultatMelding = FeilmeldingGenerator.CreateUgyldigforespoerselMelding(validationResult),
                    FileName = "payload.json",
                    MeldingsType = FeilmeldingMeldingTypeV1.Ugyldigforespørsel,
                };
            }

            return SokGenerator.CreateSokResponseMelding(sok);
        }
    }
}