using System.IO;
using System.Xml.Schema;
using KS.Fiks.IO.Arkiv.Client.Models;
using KS.Fiks.IO.Arkiv.Client.Models.Arkivering.Arkivmelding;
using KS.Fiks.IO.Arkiv.Client.Models.Innsyn.Hent;
using KS.Fiks.IO.Arkiv.Client.Models.Innsyn.Hent.Journalpost;
using ks.fiks.io.arkivsystem.sample.Handlers;
using ks.fiks.io.arkivsystem.sample.Models;
using KS.Fiks.IO.Client.Models;
using KS.Fiks.IO.Client.Models.Feilmelding;
using EksternNoekkel = KS.Fiks.IO.Arkiv.Client.Models.Arkivering.Arkivmelding.EksternNoekkel;

namespace ks.fiks.io.arkivsystem.sample.Generators
{
    public class JournalpostHentGenerator
    {
        public static JournalpostHentResultat Create(JournalpostHent journalpostHent)
        {
            var journalpost = ArkivmeldingGenerator.CreateJournalpost();
            return Create(journalpostHent, journalpost);
        }
        
        public static JournalpostHentResultat Create(JournalpostHent journalpostHent, Journalpost journalpost)
        {
            if (journalpostHent.ReferanseEksternNoekkel != null)
            {
                journalpost.ReferanseEksternNoekkel = new EksternNoekkel()
                {   
                    Fagsystem = journalpostHent.ReferanseEksternNoekkel.Fagsystem,
                    Noekkel = journalpostHent.ReferanseEksternNoekkel.Noekkel
                };
            } else if (journalpostHent.SystemID != null)
            {
                journalpost.SystemID = journalpostHent.SystemID;    
            }

            return new JournalpostHentResultat()
            {
                Journalpost = journalpost
            };
        }

        public static Melding CreateJournalpostHentResultatMelding(MottattMeldingArgs mottatt)
        {
            var journalpostHentXmlSchemaSet = new XmlSchemaSet();
            journalpostHentXmlSchemaSet.Add("http://www.arkivverket.no/standarder/noark5/journalpost/hent/v2",
                Path.Combine("Schema", "journalpostHent.xsd"));
            journalpostHentXmlSchemaSet.Add("http://www.arkivverket.no/standarder/noark5/metadatakatalog/v2",
                Path.Combine("Schema", "metadatakatalog.xsd"));

            var hentMelding = JournalpostHentHandler.GetPayload(mottatt, journalpostHentXmlSchemaSet,
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
                    : JournalpostHentGenerator.Create(hentMelding, (Journalpost)arkivmelding.Registrering[0]),
                FileName = "resultat.xml",
                MeldingsType = ArkivintegrasjonMeldingTypeV1.JournalpostHentResultat
            };
        }
    }
}