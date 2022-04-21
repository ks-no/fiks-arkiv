using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;
using KS.Fiks.Arkiv.Models.V1.Innsyn.Hent.Journalpost;

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
                journalpost.ReferanseEksternNoekkel = new KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding.EksternNoekkel()
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
    }
}