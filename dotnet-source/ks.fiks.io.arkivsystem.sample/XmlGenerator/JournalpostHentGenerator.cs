using KS.Fiks.IO.Arkiv.Client.Models.Arkivering.Arkivmelding;
using KS.Fiks.IO.Arkiv.Client.Models.Innsyn.Hent;

namespace ks.fiks.io.arkivsystem.sample.Helpers
{
    public class JournalpostHentGenerator
    {
        public static JournalpostHentResultat Create(JournalpostHent journalpostHent)
        {
            return new JournalpostHentResultat()
            {
                Journalpost = new Journalpost()
                {
                    //TODO fyll ut her
                }
            };
        }

       
    }
}