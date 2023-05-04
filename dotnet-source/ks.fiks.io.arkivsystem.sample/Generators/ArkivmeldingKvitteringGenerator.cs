using System;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmeldingkvittering;
using KS.Fiks.Arkiv.Models.V1.Metadatakatalog;

namespace ks.fiks.io.arkivsystem.sample.Generators
{
    public class ArkivmeldingKvitteringGenerator
    {
        public static ArkivmeldingKvittering CreateArkivmeldingKvittering(Arkivmelding arkivmelding)
        {
            var kvittering = new ArkivmeldingKvittering
            {
                Tidspunkt = DateTime.Now
            };
            

            if (arkivmelding?.Mappe != null)
            {
                
                kvittering.MappeKvittering.Add(CreateSaksmappeKvittering(arkivmelding.Mappe));
                
            }
            else
            {
                kvittering.RegistreringKvittering.Add(CreateJournalpostKvittering(arkivmelding.Registrering));    
            }

            return kvittering;
        }
        
        private static SaksmappeKvittering CreateSaksmappeKvittering(Mappe mappe)
        {
            var mp = new SaksmappeKvittering
            {
                SystemID = mappe.SystemID,
                OpprettetDato = DateTime.Now,
                Saksaar = DateTime.Now.Year,
                Sakssekvensnummer = new Random().Next(),
                ReferanseEksternNoekkel = new EksternNoekkel()
                {
                    Fagsystem = mappe.ReferanseEksternNoekkel.Fagsystem,
                    Noekkel = mappe.ReferanseEksternNoekkel.Noekkel
                }
            };
            if (mappe.ReferanseForeldermappe != null)
            {
                mp.ReferanseForeldermappe = new ReferanseTilMappe()
                {
                    SystemID = mappe.ReferanseForeldermappe?.SystemID,
                };
            }

            return mp;
        }

        private static RegistreringKvittering CreateJournalpostKvittering(Registrering journalpost)
        {
            var jp = new JournalpostKvittering
            {
                SystemID = journalpost.SystemID,
                Journalaar = DateTime.Now.Year,
                Journalsekvensnummer = new Random().Next(),
                Journalpostnummer = new Random().Next(1, 100),
                ReferanseEksternNoekkel = new EksternNoekkel()
                {
                    Fagsystem = journalpost.ReferanseEksternNoekkel.Fagsystem,
                    Noekkel = journalpost.ReferanseEksternNoekkel.Noekkel
                }
            };
            return jp;
        }
    }
}