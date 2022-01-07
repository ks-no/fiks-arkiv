using KS.Fiks.IO.Arkiv.Client.Models.Arkivstruktur;
using KS.Fiks.IO.Arkiv.Client.Models.Innsyn.Sok;

namespace ks.fiks.io.arkivsystem.sample.Helpers
{
    public class SokeresultatHelper
    {
        public static SokeresultatMinimum CreateSokeResultatMinimum()
        {
            var sokeResultatMinimum = new SokeresultatMinimum()
            {
                ResultatListe =
                {
                    new ResultatMinimum()
                    {
                        Mappe = new MappeMinimum()
                        {
                            Tittel = "Test1"
                        }
                    },
                    new ResultatMinimum()
                    {
                        Mappe = new MappeMinimum()
                        {
                            Tittel = "Test2"
                        }
                    }
                }
            };
            return sokeResultatMinimum;
        }

        public static Sokeresultat CreateSokeResultatUtvidet()
        {
            var sokeResultatUtvidet = new Sokeresultat()
            {
                ResultatListe =
                {
                    new Resultat()
                    {
                        Mappe = new Mappe()
                        {
                            Tittel = "Test1"
                        }
                    },
                    new Resultat()
                    {
                        Mappe = new Mappe()
                        {
                            Tittel = "Test2"
                        }
                    }
                }
            };
            return sokeResultatUtvidet;
        }

        public static SokeresultatNoekler CreateSokeResultatNoekler()
        {
            var sokeResultatNoekler = new SokeresultatNoekler()
            {
                ResultatListe =
                {
                    new ResultatNoekler()
                    {
                        Mappe = new MappeNoekler()
                        {
                            ReferanseEksternNoekkel = new EksternNoekkel()
                            {
                                Fagsystem = "Fiks-Protokoll-Validator",
                                Noekkel = "en-id-1"
                            }
                        }
                    },
                    new ResultatNoekler()
                    {
                        Mappe = new MappeNoekler()
                        {
                            ReferanseEksternNoekkel = new EksternNoekkel()
                            {
                                Fagsystem = "Fiks-Protokoll-Validator",
                                Noekkel = "en-id-2"
                            }
                        }
                    }
                }
            };
            return sokeResultatNoekler;
        }
    }
}