using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Schema;
using System.Xml.Serialization;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;
using KS.Fiks.Arkiv.Models.V1.Innsyn.Hent.Journalpost;
using KS.Fiks.Arkiv.Models.V1.Innsyn.Hent.Mappe;
using KS.Fiks.Arkiv.Models.V1.Meldingstyper;
using ks.fiks.io.arkivsystem.sample.Generators;
using ks.fiks.io.arkivsystem.sample.Models;
using KS.Fiks.IO.Client.Models;
using KS.Fiks.IO.Client.Models.Feilmelding;
using Serilog;

namespace ks.fiks.io.arkivsystem.sample.Handlers
{
    public class MappeHentHandler : BaseHandler
    {
        private static readonly ILogger Log = Serilog.Log.ForContext(MethodBase.GetCurrentMethod()?.DeclaringType);
        
        private MappeHent GetPayload(MottattMeldingArgs mottatt, XmlSchemaSet xmlSchemaSet,
            out bool xmlValidationErrorOccured, out List<List<string>> validationResult)
        {
            if (mottatt.Melding.HasPayload)
            {
                var text = GetPayloadAsString(mottatt, xmlSchemaSet, out xmlValidationErrorOccured,
                    out validationResult);
                Log.Information("Parsing mappeHent: {Xml}", text);
                if (string.IsNullOrEmpty(text))
                {
                    Log.Error("Tom mappeHent? Xml: {Xml}", text);
                }

                using var textReader = (TextReader)new StringReader(text);
                return(MappeHent) new XmlSerializer(typeof(MappeHent)).Deserialize(textReader);
            }

            xmlValidationErrorOccured = false;
            validationResult = null;
            return null;
        }

        public Melding HandleMelding(MottattMeldingArgs mottatt)
        {
            var hentMelding = GetPayload(mottatt, XmlSchemaSet,
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

            var lagretArkivmelding = TryGetLagretArkivmelding(mottatt);

            if (lagretArkivmelding != null && lagretArkivmelding.Mappe.Count <= 0)
            {
                return new Melding
                {
                    ResultatMelding = "Kunne ikke finne noen mappe som tilsvarer det som er etterspurt i hentmelding",
                    FileName = "payload.json",
                    MeldingsType = FeilmeldingMeldingTypeV1.Ugyldigforespørsel,
                };
            }

            return new Melding
            {
                ResultatMelding = lagretArkivmelding == null
                    ? MappeHentGenerator.Create(hentMelding)
                    : MappeHentGenerator.CreateFromCache(hentMelding, lagretArkivmelding),
                FileName = "resultat.xml",
                MeldingsType = FiksArkivV1Meldingtype.MappeHentResultat
            };
        }
    }
}