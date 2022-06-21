using System;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmeldingkvittering;

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
            var isMappe = arkivmelding?.Mappe?.Count > 0;

            if (isMappe)
            {
                foreach (var mappe in arkivmelding.Mappe)
                {
                    kvittering.MappeKvittering.Add(CreateSaksmappeKvittering(mappe));
                }
                
            }
            else
            {
                foreach (var registrering in arkivmelding.Registrering)
                {
                    kvittering.RegistreringKvittering.Add(CreateJournalpostKvittering(registrering));    
                }
                
            }

            return kvittering;
        }
        
        private static SaksmappeKvittering CreateSaksmappeKvittering(Mappe mappe)
        {
            var mp = new SaksmappeKvittering
            {
                SystemID = mappe.SystemID,
                OpprettetDato = DateTime.Now,
                Saksaar = DateTime.Now.Year.ToString(),
                Sakssekvensnummer = new Random().Next().ToString(),
                ReferanseForeldermappe = mappe.ReferanseForeldermappe?.SystemID,
                ReferanseEksternNoekkel = mappe.ReferanseEksternNoekkel,
            };
            return mp;
        }

        private static RegistreringKvittering CreateJournalpostKvittering(Registrering journalpost)
        {
            var jp = new JournalpostKvittering
            {
                SystemID = journalpost.SystemID,
                Journalaar = DateTime.Now.Year.ToString(),
                Journalsekvensnummer = new Random().Next().ToString(),
                Journalpostnummer = new Random().Next(1, 100).ToString(),
                ReferanseEksternNoekkel = journalpost.ReferanseEksternNoekkel,
            };
            return jp;
        }
    }
}