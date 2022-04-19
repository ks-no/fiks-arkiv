using System;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmeldingkvittering;
using KS.Fiks.Arkiv.Models.V1.Metadatakatalog;
using EksternNoekkel = KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmeldingkvittering.EksternNoekkel;

namespace ks.fiks.io.arkivsystem.sample.Generators
{
    public class ArkivmeldingKvitteringGenerator
    {
        public static SaksmappeKvittering CreateSaksmappeKvittering()
        {
            var mp = new SaksmappeKvittering
            {
                SystemID = new SystemID
                {
                    Value = Guid.NewGuid().ToString()
                },
                OpprettetDato = DateTime.Now,
                Saksaar = DateTime.Now.Year.ToString(),
                Sakssekvensnummer = new Random().Next().ToString()
            };
            return mp;
        }

        public static JournalpostKvittering CreateJournalpostKvittering(Arkivmelding arkivmelding)
        {
            var jp = new JournalpostKvittering
            {
                SystemID = new SystemID
                {
                    Value = Guid.NewGuid().ToString()
                },
                Journalaar = DateTime.Now.Year.ToString(),
                Journalsekvensnummer = new Random().Next().ToString(),
                Journalpostnummer = new Random().Next(1, 100).ToString(),
                ReferanseEksternNoekkel = new EksternNoekkel()
                {
                    Fagsystem = arkivmelding.Registrering[0].ReferanseEksternNoekkel.Fagsystem,
                    Noekkel = arkivmelding.Registrering[0].ReferanseEksternNoekkel.Noekkel
                }
            };
            return jp;
        }
    }
}