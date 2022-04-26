using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;
using KS.Fiks.Arkiv.Models.V1.Innsyn.Hent.Journalpost;
using KS.Fiks.Arkiv.Models.V1.Meldingstyper;
using ks.fiks.io.arkivsystem.sample.Generators;
using ks.fiks.io.arkivsystem.sample.Models;
using KS.Fiks.IO.Client.Models;
using KS.Fiks.IO.Client.Models.Feilmelding;
using Serilog;

namespace ks.fiks.io.arkivsystem.sample.Handlers
{
    public class JournalpostHentHandler : BaseHandler
    {
        private static readonly ILogger Log = Serilog.Log.ForContext(MethodBase.GetCurrentMethod()?.DeclaringType);
        private readonly XmlSchemaSet _journalpostHentXmlSchemaSet;
        
        public JournalpostHentHandler()
        {
            _journalpostHentXmlSchemaSet = new XmlSchemaSet();
            var arkivModelsAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .SingleOrDefault(assembly => assembly.GetName().Name == "KS.Fiks.Arkiv.Models.V1");
            
            using (var schemaStream = arkivModelsAssembly?.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.journalpostHent.xsd"))
            {
                if (schemaStream != null)
                {
                    using var schemaReader = XmlReader.Create(schemaStream);
                    _journalpostHentXmlSchemaSet.Add(
                        "http://www.arkivverket.no/standarder/noark5/journalpost/hent/v2", schemaReader);
                }
            }
            using (var schemaStream = arkivModelsAssembly?.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.metadatakatalog.xsd"))
            {
                if (schemaStream != null)
                {
                    using var schemaReader = XmlReader.Create(schemaStream);
                    _journalpostHentXmlSchemaSet.Add(
                        "http://www.arkivverket.no/standarder/noark5/metadatakatalog/v2", schemaReader);
                }
            }
            using (var schemaStream = arkivModelsAssembly?.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.arkivstruktur.xsd"))
            {
                if (schemaStream != null)
                {
                    using var schemaReader = XmlReader.Create(schemaStream);
                    _journalpostHentXmlSchemaSet.Add("http://www.arkivverket.no/standarder/noark5/arkivstruktur",
                        schemaReader);
                }
            }
        }

        private JournalpostHent GetPayload(MottattMeldingArgs mottatt, XmlSchemaSet xmlSchemaSet,
            out bool xmlValidationErrorOccured, out List<List<string>> validationResult)
        {
            if (mottatt.Melding.HasPayload)
            {
                var text = GetPayloadAsString(mottatt, xmlSchemaSet, out xmlValidationErrorOccured,
                    out validationResult);
                Log.Information("Parsing journalpostHent: {Xml}", text);
                if (string.IsNullOrEmpty(text))
                {
                    Log.Error("Tom journalpostHent? Xml: {Xml}", text);
                }

                using var textReader = (TextReader)new StringReader(text);
                return(JournalpostHent) new XmlSerializer(typeof(JournalpostHent)).Deserialize(textReader);
            }

            xmlValidationErrorOccured = false;
            validationResult = null;
            return null;
        }

        public Melding HandleMelding(MottattMeldingArgs mottatt)
        {
            var hentMelding = GetPayload(mottatt, _journalpostHentXmlSchemaSet,
                out var xmlValidationErrorOccured, out var validationResult);

            if (xmlValidationErrorOccured)
            {
                return new Melding
                {
                    ResultatMelding = FeilmeldingGenerator.CreateUgyldigforespoerselMelding(validationResult),
                    FileName = "payload.json",
                    MeldingsType = FeilmeldingMeldingTypeV1.Ugyldigforespørsel,
                };
            }

            // Hent arkivmelding fra "cache" hvis det er en testSessionId i headere
            Arkivmelding arkivmelding = null;
            if (mottatt.Melding.Headere.TryGetValue(ArkivSimulator.TestSessionIdHeader, out var testSessionId))
            {
                ArkivSimulator._arkivmeldingCache.TryGetValue(testSessionId, out arkivmelding);
            }

            return new Melding
            {
                ResultatMelding = arkivmelding == null
                    ? JournalpostHentGenerator.Create(hentMelding)
                    : JournalpostHentGenerator.Create(hentMelding, JournalpostHentGenerator.CreateHentJournalpostArkivmeldingJournalpost((Journalpost) arkivmelding.Registrering[0])),
                FileName = "resultat.xml",
                MeldingsType = FiksArkivV1Meldingtype.JournalpostHentResultat
            };
        }
    }
}