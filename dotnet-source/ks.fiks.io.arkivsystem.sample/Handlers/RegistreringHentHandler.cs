using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Schema;
using System.Xml.Serialization;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;
using KS.Fiks.Arkiv.Models.V1.Innsyn.Hent.Registrering;
using KS.Fiks.Arkiv.Models.V1.Meldingstyper;
using ks.fiks.io.arkivsystem.sample.Generators;
using ks.fiks.io.arkivsystem.sample.Models;
using KS.Fiks.IO.Client.Models;
using Serilog;

namespace ks.fiks.io.arkivsystem.sample.Handlers
{
    public class JournalpostHentHandler : BaseHandler
    {
        private static readonly ILogger Log = Serilog.Log.ForContext(MethodBase.GetCurrentMethod()?.DeclaringType);
        
        private RegistreringHent GetPayload(MottattMeldingArgs mottatt, XmlSchemaSet xmlSchemaSet,
            out bool xmlValidationErrorOccured, out List<List<string>> validationResult)
        {
            if (mottatt.Melding.HasPayload)
            {
                var text = GetPayloadAsString(mottatt, xmlSchemaSet, out xmlValidationErrorOccured,
                    out validationResult);
                Log.Information("Parsing journalpostHent: {Xml}", text);
                if (string.IsNullOrEmpty(text))
                {
                    Log.Error("Tom registreringHent? Xml: {Xml}", text);
                }

                using var textReader = (TextReader)new StringReader(text);
                return(RegistreringHent) new XmlSerializer(typeof(RegistreringHent)).Deserialize(textReader);
            }

            xmlValidationErrorOccured = false;
            validationResult = null;
            return null;
        }

        private bool HarJournalpost(Arkivmelding lagretArkivmelding, RegistreringHent registreringHent)
        {
            if (lagretArkivmelding == null)
            {
                return false;
            }

            if (lagretArkivmelding.Registrering.ReferanseEksternNoekkel ==
                registreringHent.ReferanseTilRegistrering.ReferanseEksternNoekkel ||
                lagretArkivmelding.Registrering.SystemID == registreringHent.ReferanseTilRegistrering.SystemID)
            {
                return true;
            }

            return false;
        }
        
        private Journalpost GetJournalpost(Arkivmelding lagretArkivmelding, RegistreringHent registreringHent)
        {
            if (lagretArkivmelding.Mappe != null)
            {
                return null;
            }

            if (lagretArkivmelding.Registrering != null)
            {
                if(AreEqual(lagretArkivmelding.Registrering, registreringHent.ReferanseTilRegistrering.ReferanseEksternNoekkel, registreringHent.ReferanseTilRegistrering.SystemID)) {
                    return (Journalpost) lagretArkivmelding.Registrering;
                }
            }
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
                    FileName = "feilmelding.xml",
                    MeldingsType = FiksArkivMeldingtype.Ugyldigforespørsel,
                };
            }

            // Forsøk å hente arkivmelding fra lokal lagring
            var lagretArkivmelding = TryGetLagretArkivmelding(mottatt);
            
            if (!HarJournalpost(lagretArkivmelding, hentMelding))
            {
                return new Melding
                {
                    ResultatMelding = FeilmeldingGenerator.CreateIkkefunnetMelding("Kunne ikke finne noen journalpost som tilsvarer det som er etterspurt i hentmelding"),
                    FileName = "feilmelding.xml",
                    MeldingsType = FiksArkivMeldingtype.Ikkefunnet,
                };
            }

            return new Melding
            {
                ResultatMelding = lagretArkivmelding == null
                    ? RegistreringHentResultatGenerator.Create(hentMelding)
                    : RegistreringHentResultatGenerator.Create(hentMelding, RegistreringHentResultatGenerator.CreateHentJournalpostFraArkivmeldingJournalpost(GetJournalpost(lagretArkivmelding, hentMelding))),
                FileName = "resultat.xml",
                MeldingsType = FiksArkivMeldingtype.RegistreringHentResultat
            };
        }
    }
}