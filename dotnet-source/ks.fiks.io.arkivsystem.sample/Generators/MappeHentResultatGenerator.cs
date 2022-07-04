using System;
using System.Linq;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;
using KS.Fiks.Arkiv.Models.V1.Innsyn.Hent.Mappe;
using KS.Fiks.Arkiv.Models.V1.Kodelister;
using KS.Fiks.Arkiv.Models.V1.Metadatakatalog;
using EksternNoekkel = KS.Fiks.Arkiv.Models.V1.Arkivstruktur.EksternNoekkel;
using Gradering = KS.Fiks.Arkiv.Models.V1.Arkivstruktur.Gradering;
using Mappe = KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding.Mappe;
using Saksmappe = KS.Fiks.Arkiv.Models.V1.Arkivstruktur.Saksmappe;
using Skjerming = KS.Fiks.Arkiv.Models.V1.Arkivstruktur.Skjerming;

namespace ks.fiks.io.arkivsystem.sample.Generators
{
    public class MappeHentResultatGenerator
    {
        public static MappeHentResultat Create(MappeHent mappeHent)
        {
            return new MappeHentResultat()
            {
                Mappe = CreateMappe(mappeHent)
            };
        }
        
        public static MappeHentResultat CreateFromCache(MappeHent mappeHent, Arkivmelding arkivmeldingFraCache)
        {
            var arkivmeldingMappe = arkivmeldingFraCache.Mappe.FirstOrDefault(mappeFraCache => AreEqual(mappeHent, mappeFraCache));
            
            if (arkivmeldingMappe is KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding.Saksmappe mappe)
            {
                return new MappeHentResultat
                {
                    Mappe = MapFromArkivmeldingMappe(mappe)
                };
                
            }
            return new MappeHentResultat
            {
                Mappe = MapFromArkivmeldingMappe(arkivmeldingMappe)
            };
        }

        private static bool AreEqual(MappeHent mappeHent, Mappe mappeFraCache)
        {
            if (mappeHent.ReferanseEksternNoekkel != null && mappeFraCache.ReferanseEksternNoekkel != null &&
                mappeHent.ReferanseEksternNoekkel.Noekkel == mappeFraCache.ReferanseEksternNoekkel.Noekkel)
            {
                return true;
            }

            return mappeHent.SystemID != null && mappeFraCache.SystemID != null && mappeHent.SystemID == mappeFraCache.SystemID;
        }

        public static KS.Fiks.Arkiv.Models.V1.Arkivstruktur.Mappe CreateMappe(MappeHent mappeHent)
        {
            return new KS.Fiks.Arkiv.Models.V1.Arkivstruktur.Mappe()
            {
                
            };
        }

        public static KS.Fiks.Arkiv.Models.V1.Arkivstruktur.Mappe MapFromArkivmeldingMappe(Mappe arkivmeldingMappe)
        {
            var mappe = new KS.Fiks.Arkiv.Models.V1.Arkivstruktur.Mappe()
            {
                SystemID = arkivmeldingMappe.SystemID ?? new SystemID()
                {
                    Value = Guid.NewGuid().ToString()
                },
                MappeID = arkivmeldingMappe.MappeID ?? Guid.NewGuid().ToString(),
                Tittel = arkivmeldingMappe.Tittel,
                OffentligTittel = arkivmeldingMappe.OffentligTittel,
                Beskrivelse = arkivmeldingMappe.Beskrivelse,
                Skjerming = arkivmeldingMappe.Skjerming != null ? mapToSkjerming(arkivmeldingMappe) : null,
                Gradering = arkivmeldingMappe.Gradering != null ? mapToGradering(arkivmeldingMappe) : null,
                AvsluttetAv = arkivmeldingMappe.AvsluttetAv ?? "Avsluttet Av",
                AvsluttetDato = arkivmeldingMappe.AvsluttetDato,
                OpprettetAv = arkivmeldingMappe.OpprettetAv ?? "Opprettet Av",
                OpprettetDato = arkivmeldingMappe.OpprettetDato
                //Kassasjon = arkivmeldingMappe //TODO mangler i arkivstruktur
            };
            if (arkivmeldingMappe.Dokumentmedium != null)
            {
                mappe.Dokumentmedium = new Dokumentmedium()
                {
                    Beskrivelse = arkivmeldingMappe.Dokumentmedium.Beskrivelse,
                    KodeProperty = arkivmeldingMappe.Dokumentmedium.KodeProperty
                };
            }

            return mappe;
        }

        private static Gradering mapToGradering(Mappe arkivmeldingMappe)
        {
            return new Gradering()
            {
                Grad  = arkivmeldingMappe.Gradering.Grad,
                Graderingsdato = arkivmeldingMappe.Gradering.Graderingsdato,
                GradertAv = arkivmeldingMappe.Gradering.GradertAv,
                Nedgraderingsdato = arkivmeldingMappe.Gradering.Nedgraderingsdato,
                NedgraderingsdatoSpecified = arkivmeldingMappe.Gradering.NedgraderingsdatoSpecified,
                NedgradertAv = arkivmeldingMappe.Gradering.NedgradertAv
            };
        }

        private static Skjerming mapToSkjerming(Mappe arkivmeldingMappe)
        {
            return new Skjerming()
            {
                SkjermingOpphoererAksjon = arkivmeldingMappe.Skjerming.SkjermingOpphoererAksjon,
                SkjermingOpphoererDato = arkivmeldingMappe.Skjerming.SkjermingOpphoererDato,
                SkjermingOpphoererDatoSpecified = arkivmeldingMappe.Skjerming.SkjermingOpphoererDatoSpecified,
                Skjermingshjemmel = arkivmeldingMappe.Skjerming.Skjermingshjemmel,
                Tilgangsrestriksjon = arkivmeldingMappe.Skjerming.Tilgangsrestriksjon
            };
        }

        public static Saksmappe MapFromArkivmeldingMappe(KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding.Saksmappe arkivmeldingMappe)
        {
            var mappe = new Saksmappe()
            {
                SystemID = arkivmeldingMappe.SystemID ?? new SystemID()
                {
                    Value = Guid.NewGuid().ToString()
                },
                MappeID = arkivmeldingMappe.MappeID ?? Guid.NewGuid().ToString(),
                Tittel = arkivmeldingMappe.Tittel,
                OffentligTittel = arkivmeldingMappe.OffentligTittel,
                Beskrivelse = arkivmeldingMappe.Beskrivelse,
                Skjerming = arkivmeldingMappe.Skjerming != null ? mapToSkjerming(arkivmeldingMappe) : null,
                Gradering = arkivmeldingMappe.Gradering != null ? mapToGradering(arkivmeldingMappe) : null,
                AvsluttetAv = arkivmeldingMappe.AvsluttetAv ?? "Avsluttet Av",
                AvsluttetDato = arkivmeldingMappe.AvsluttetDato,
                OpprettetAv = arkivmeldingMappe.OpprettetAv ?? "Opprettet Av",
                OpprettetDato = arkivmeldingMappe.OpprettetDato,
                //Kassasjon = arkivmeldingMappe //TODO mangler i arkivstruktur
                Saksaar = arkivmeldingMappe.Saksaar,
                Saksdato = arkivmeldingMappe.Saksdato,
                Sakssekvensnummer = arkivmeldingMappe.Sakssekvensnummer,
                AdministrativEnhet = arkivmeldingMappe.AdministrativEnhet ?? "Administrativ enhet",
                Saksansvarlig = arkivmeldingMappe.Saksansvarlig ?? "Default Saksansvarlig",
                
            };

            if (arkivmeldingMappe.ReferanseForeldermappe != null)
            {
                mappe.ReferanseForeldermappe = new SystemID()
                {
                    Label = arkivmeldingMappe.ReferanseForeldermappe.SystemID.Label,
                    Value = arkivmeldingMappe.ReferanseForeldermappe.SystemID.Value
                };
            }

            if (arkivmeldingMappe.ReferanseEksternNoekkel != null)
            {
                mappe.ReferanseEksternNoekkel = new EksternNoekkel()
                {
                    Fagsystem = arkivmeldingMappe.ReferanseEksternNoekkel.Fagsystem,
                    Noekkel = arkivmeldingMappe.ReferanseEksternNoekkel.Noekkel
                };
            }

            if (arkivmeldingMappe.Saksstatus != null)
            {
                mappe.Saksstatus = new Saksstatus()
                {
                    Beskrivelse = arkivmeldingMappe.Saksstatus.Beskrivelse,
                    KodeProperty = arkivmeldingMappe.Saksstatus.KodeProperty
                };
            }
            else
            {
                mappe.Saksstatus = new Saksstatus()
                {
                    Beskrivelse = SaksstatusKoder.UnderBehandling.Beskrivelse,
                    KodeProperty = SaksstatusKoder.UnderBehandling.Verdi
                };
            }
            
            
            if (arkivmeldingMappe.Dokumentmedium != null)
            {
                mappe.Dokumentmedium = new Dokumentmedium()
                {
                    Beskrivelse = arkivmeldingMappe.Dokumentmedium.Beskrivelse,
                    KodeProperty = arkivmeldingMappe.Dokumentmedium.KodeProperty
                };
            }

            return mappe;
        }
    }
}