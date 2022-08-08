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
using ks.fiks.io.arkivsystem.sample.Storage;
using KS.Fiks.IO.Client.Models;
using Serilog;

namespace ks.fiks.io.arkivsystem.sample.Handlers
{
    public class JournalpostHentHandler : BaseHandler, IMeldingHandler
    {
        private static readonly ILogger Log = Serilog.Log.ForContext(MethodBase.GetCurrentMethod()?.DeclaringType);

        public JournalpostHentHandler(IArkivmeldingCache arkivmeldingCache) : base(arkivmeldingCache)
        {
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

        private Arkivmelding GetArkivmeldingMedJournalpost(List<Arkivmelding> lagretArkivmeldinger, JournalpostHent journalpostHent)
        {
            if (lagretArkivmeldinger == null)
            {
                return null;
            }

            foreach (var lagretArkivmelding in lagretArkivmeldinger)
            {
                if (lagretArkivmelding.Mappe.Count >= 0)
                {
                    foreach (var mappe in lagretArkivmelding.Mappe)
                    {
                        foreach (var registrering in mappe.Registrering)
                        {
                            if (AreEqual(registrering, journalpostHent.ReferanseEksternNoekkel,
                                    journalpostHent.SystemID))
                            {
                                return lagretArkivmelding;
                            }
                        }
                    }
                }

                if(lagretArkivmelding.Registrering.OfType<Journalpost>().Any(registrering =>
                    AreEqual(registrering, journalpostHent.ReferanseEksternNoekkel, journalpostHent.SystemID)))
                {
                    return lagretArkivmelding;
                }
            }
            return null;
        }
        
        private Journalpost GetJournalpost(Arkivmelding lagretArkivmelding, JournalpostHent journalpostHent)
        {
            if (lagretArkivmelding.Mappe.Count >= 0)
            {
                foreach (var mappe in lagretArkivmelding.Mappe)
                {
                    foreach (var registrering in mappe.Registrering)
                    {
                        if (AreEqual(registrering, journalpostHent.ReferanseEksternNoekkel, journalpostHent.SystemID))
                        {
                            return (Journalpost) registrering;
                        }
                    }
                }
            }
            if (lagretArkivmelding.Registrering.Count >= 0)
            {
                foreach (var registrering in lagretArkivmelding.Registrering)
                {
                    if (AreEqual(registrering, journalpostHent.ReferanseEksternNoekkel, journalpostHent.SystemID))
                    {
                        return (Journalpost) registrering;
                    }
                }
            }
            return null;
        }

        public List<Melding> HandleMelding(MottattMeldingArgs mottatt)
        {
            var meldinger = new List<Melding>();
            
            var hentMelding = GetPayload(mottatt, XmlSchemaSet,
                out var xmlValidationErrorOccured, out var validationResult);

            if (xmlValidationErrorOccured)
            {
                meldinger.Add(new Melding
                {
                    ResultatMelding = FeilmeldingGenerator.CreateUgyldigforespoerselMelding(validationResult),
                    FileName = "feilmelding.xml",
                    MeldingsType = FiksArkivMeldingtype.Ugyldigforespørsel,
                });
                return meldinger;
            }

            // Forsøk å hente arkivmelding fra lokal lagring
            var lagretArkivmeldinger = TryGetLagretArkivmeldinger(mottatt);
            var lagretArkivmelding = GetArkivmeldingMedJournalpost(lagretArkivmeldinger, hentMelding);
            
            if (lagretArkivmelding == null)
            {
                meldinger.Add(new Melding
                {
                    ResultatMelding = FeilmeldingGenerator.CreateIkkefunnetMelding("Kunne ikke finne noen journalpost som tilsvarer det som er etterspurt i hentmelding"),
                    FileName = "feilmelding.xml",
                    MeldingsType = FiksArkivMeldingtype.Ikkefunnet,
                });
                return meldinger;
            }

            meldinger.Add(new Melding
            {
                ResultatMelding = lagretArkivmelding == null
                    ? JournalpostHentResultatGenerator.Create(hentMelding)
                    : JournalpostHentResultatGenerator.Create(hentMelding, JournalpostHentResultatGenerator.CreateHentJournalpostFraArkivmeldingJournalpost(GetJournalpost(lagretArkivmelding, hentMelding))),
                FileName = "resultat.xml",
                MeldingsType = FiksArkivMeldingtype.JournalpostHentResultat
            });

            return meldinger;
        }
    }
}