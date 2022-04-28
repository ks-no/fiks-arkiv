using System;
using System.Linq;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;
using KS.Fiks.Arkiv.Models.V1.Innsyn.Hent.Mappe;
using KS.Fiks.Arkiv.Models.V1.Kodelister;
using KS.Fiks.Arkiv.Models.V1.Metadatakatalog;

namespace ks.fiks.io.arkivsystem.sample.Generators
{
    public class MappeHentGenerator
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
            KS.Fiks.Arkiv.Models.V1.Arkivstruktur.Mappe returnMappe;
            
            if (arkivmeldingMappe is Saksmappe mappe)
            {
                returnMappe = MapFromArkivmeldingMappe(mappe);
            }
            else
            {
                returnMappe = MapFromArkivmeldingMappe(arkivmeldingMappe);
            }
            return new MappeHentResultat
            {
                Mappe = returnMappe
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
                Skjerming = arkivmeldingMappe.Skjerming,
                Gradering = arkivmeldingMappe.Gradering,
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
        
        public static KS.Fiks.Arkiv.Models.V1.Arkivstruktur.Saksmappe MapFromArkivmeldingMappe(Saksmappe arkivmeldingMappe)
        {
            var mappe = new KS.Fiks.Arkiv.Models.V1.Arkivstruktur.Saksmappe()
            {
                SystemID = arkivmeldingMappe.SystemID ?? new SystemID()
                {
                    Value = Guid.NewGuid().ToString()
                },
                MappeID = arkivmeldingMappe.MappeID ?? Guid.NewGuid().ToString(),
                Tittel = arkivmeldingMappe.Tittel,
                OffentligTittel = arkivmeldingMappe.OffentligTittel,
                Beskrivelse = arkivmeldingMappe.Beskrivelse,
                Skjerming = arkivmeldingMappe.Skjerming,
                Gradering = arkivmeldingMappe.Gradering,
                AvsluttetAv = arkivmeldingMappe.AvsluttetAv ?? "Avsluttet Av",
                AvsluttetDato = arkivmeldingMappe.AvsluttetDato,
                OpprettetAv = arkivmeldingMappe.OpprettetAv ?? "Opprettet Av",
                OpprettetDato = arkivmeldingMappe.OpprettetDato,
                //Kassasjon = arkivmeldingMappe //TODO mangler i arkivstruktur
                Saksaar = arkivmeldingMappe.Saksaar ?? DateTime.Now.Year.ToString(),
                Saksdato = arkivmeldingMappe.Saksdato,
                Sakssekvensnummer = arkivmeldingMappe.Sakssekvensnummer ?? "1",
                AdministrativEnhet = arkivmeldingMappe.AdministrativEnhet ?? "Administrativ enhet",
                Saksansvarlig = arkivmeldingMappe.Saksansvarlig ?? "Default Saksansvarlig",
                
            };

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