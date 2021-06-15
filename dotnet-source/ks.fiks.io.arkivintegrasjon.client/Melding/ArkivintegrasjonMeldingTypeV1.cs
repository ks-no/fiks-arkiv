using System.Collections.Generic;

namespace ks.fiks.io.arkivintegrasjon.client.Melding
{
    public static class ArkivintegrasjonMeldingTypeV1
    {
        // Arkivintegrasjon mottaksmelding og kvitteringsmelding
        public const string Mottatt = "no.ks.fiks.gi.arkivintegrasjon.mottatt.v1";
        public const string Kvittering = "no.ks.fiks.gi.arkivintegrasjon.kvittering.v1";
        
        // Basis
        public const string BasisArkivmelding = "no.ks.fiks.gi.arkivintegrasjon.oppdatering.basis.arkivmelding.v1";
        public const string BasisArkivmeldingUtgaaende = "no.ks.fiks.gi.arkivintegrasjon.oppdatering.basis.arkivmeldingUtgaaende.v1";
        public const string ForenkletArkivmeldingInnkommende = "no.ks.fiks.gi.arkivintegrasjon.oppdatering.forenklet.arkivmeldingInnkommende.v1";
        public const string BasisOppdaterSaksmappe = "no.ks.fiks.gi.arkivintegrasjon.oppdatering.basis.oppdatersaksmappe.v1";
        
        // Sok
        public const string InnsynSok = "no.ks.fiks.gi.arkivintegrasjon.innsyn.sok.v1";
        public const string InnsynSokResultat = "no.ks.fiks.gi.arkivintegrasjon.innsyn.sok.resultat.v1";
        
        // Avansert
        public const string OppdateringArkivmelding = "no.ks.fiks.gi.arkivintegrasjon.oppdatering.arkivmelding.v1";
        public const string OppdateringArkivmeldingUtgaaende =
            "no.ks.fiks.gi.arkivintegrasjon.oppdatering.arkivmeldingUtgaaende.v1";
        
        public static readonly List<string> Basis = new List<string>()
        {
            BasisArkivmelding,
            BasisArkivmeldingUtgaaende,
            ForenkletArkivmeldingInnkommende,
            BasisOppdaterSaksmappe
        };
            
        public static readonly List<string> Sok = new List<string>()
        {
            InnsynSok,
            InnsynSokResultat
        };

        public static readonly List<string> Avansert = new List<string>()
        {
            OppdateringArkivmelding,
            OppdateringArkivmeldingUtgaaende
        };

        public static bool IsBasis(string meldingsType)
        {
            return Basis.Contains(meldingsType);
        }

        public static bool IsSok(string meldingsType)
        {
            return Sok.Contains(meldingsType);
        }
        
        public static bool IsAvansert(string meldingsType)
        { 
            return Avansert.Contains(meldingsType);
        }
    }
}