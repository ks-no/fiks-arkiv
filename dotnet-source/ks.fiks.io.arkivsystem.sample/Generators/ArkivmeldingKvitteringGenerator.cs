using System;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmeldingkvittering;
using KS.Fiks.Arkiv.Models.V1.Arkivstruktur;
using EksternNoekkel = KS.Fiks.Arkiv.Models.V1.Arkivstruktur.EksternNoekkel;
using Mappe = KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding.Mappe;
using Registrering = KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding.Registrering;

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
                Saksaar = DateTime.Now.Year,
                Sakssekvensnummer = new Random().Next(),
                ReferanseForeldermappe = mappe.ReferanseForeldermappe?.SystemID,
                ReferanseEksternNoekkel = new EksternNoekkel()
                {
                    Fagsystem = mappe.ReferanseEksternNoekkel.Fagsystem,
                    Noekkel = mappe.ReferanseEksternNoekkel.Noekkel
                }
            };
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