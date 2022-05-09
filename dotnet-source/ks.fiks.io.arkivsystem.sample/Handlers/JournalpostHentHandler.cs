using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Schema;
using System.Xml.Serialization;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;
using KS.Fiks.Arkiv.Models.V1.Innsyn.Hent.Journalpost;
using KS.Fiks.Arkiv.Models.V1.Meldingstyper;
using ks.fiks.io.arkivsystem.sample.Generators;
using ks.fiks.io.arkivsystem.sample.Models;
using KS.Fiks.IO.Client.Models;
using KS.Fiks.Protokoller.V1.Models.Feilmelding;
using Serilog;

namespace ks.fiks.io.arkivsystem.sample.Handlers
{
    public class JournalpostHentHandler : BaseHandler
    {
        private static readonly ILogger Log = Serilog.Log.ForContext(MethodBase.GetCurrentMethod()?.DeclaringType);
        
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

        private bool HarJournalpost(Arkivmelding lagretArkivmelding, JournalpostHent journalpostHent)
        {
            return lagretArkivmelding.Registrering.OfType<Journalpost>().Any(registrering => AreEqual(registrering, journalpostHent.ReferanseEksternNoekkel, journalpostHent.SystemID));
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
                    MeldingsType = FeilmeldingType.Ugyldigforespørsel,
                };
            }

            // Forsøk å hente arkivmelding fra lokal lagring
            var lagretArkivmelding = TryGetLagretArkivmelding(mottatt);
            
            if (lagretArkivmelding != null && (lagretArkivmelding.Registrering.Count <= 0 || !HarJournalpost(lagretArkivmelding, hentMelding)))
            {
                return new Melding
                {
                    ResultatMelding = "Kunne ikke finne noen journalpost som tilsvarer det som er etterspurt i hentmelding",
                    FileName = "payload.json",
                    MeldingsType = FeilmeldingType.Ikkefunnet,
                };
            }

            return new Melding
            {
                ResultatMelding = lagretArkivmelding == null
                    ? JournalpostHentResultatGenerator.Create(hentMelding)
                    : JournalpostHentResultatGenerator.Create(hentMelding, JournalpostHentResultatGenerator.CreateHentJournalpostFraArkivmeldingJournalpost((Journalpost) lagretArkivmelding.Registrering[0])),
                FileName = "resultat.xml",
                MeldingsType = FiksArkivV1Meldingtype.JournalpostHentResultat
            };
        }
    }
}