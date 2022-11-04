using System;
using KS.Fiks.Arkiv.Models.V1.Arkivstruktur;
using KS.Fiks.Arkiv.Models.V1.Arkivstruktur.Minimum;
using KS.Fiks.Arkiv.Models.V1.Arkivstruktur.Noekler;
using KS.Fiks.Arkiv.Models.V1.Innsyn.Sok;
using KS.Fiks.Arkiv.Models.V1.Metadatakatalog;

namespace ks.fiks.io.arkivsystem.sample.Helpers
{
    public class SokeresultatGenerator
    {
        public static SokeresultatMinimum CreateSokeResultatMinimum(Sokdefinisjon sokeSokdefinisjon)
        {
            var sokeResultatMinimum = new SokeresultatMinimum()
            {
                System = "",
                Take = 10,
                Skip = 0,
                Tidspunkt = DateTime.Now,
                
            };

            switch (sokeSokdefinisjon)
            {
                case MappeSokdefinisjon:
                    AddMappeResultatMinimum(sokeResultatMinimum);
                    break;
                case SaksmappeSokdefinisjon:
                    AddSaksmappeResultatMinimum(sokeResultatMinimum);
                    break;
            }
            return sokeResultatMinimum;
        }

        private static void AddSaksmappeResultatMinimum(SokeresultatMinimum sokeresultatMinimum)
        {
            sokeresultatMinimum.ResultatListe.Add(
                new ResultatMinimum()
                {
                    Saksmappe = new SaksmappeMinimum()
                    {
                        SystemID = new SystemID() { Label = "", Value = "02379f7b-b99f-44e6-a711-822f00002587" },
                        MappeID = "Test",
                        Tittel = "Test1",
                        Saksaar = DateTime.Now.Year,
                        Sakssekvensnummer = 1,
                        Saksdato = DateTime.Now,
                        AdministrativEnhet = new AdministrativEnhet()
                        {
                            Navn = "Test"
                        },
                        Saksansvarlig = new Saksansvarlig()
                        {
                            Navn = "Test"
                        },
                        Saksstatus = new Saksstatus()
                        {
                            KodeProperty= "Test"
                        }
                    }
                });
            sokeresultatMinimum.ResultatListe.Add(
                new ResultatMinimum()
                {
                    Saksmappe = new SaksmappeMinimum()
                    {
                        SystemID = new SystemID() { Label = "", Value = "02379f7b-b99f-44e6-a711-822f00002587" },
                        MappeID = "Test",
                        Tittel = "Test2",
                        Saksaar = DateTime.Now.Year,
                        Sakssekvensnummer = 1,
                        Saksdato = DateTime.Now,
                        AdministrativEnhet = new AdministrativEnhet()
                        {
                            Navn = "Test"
                        },
                        Saksansvarlig = new Saksansvarlig() {
                            Navn = "Test"
                        },
                        Saksstatus = new Saksstatus()
                        {
                            KodeProperty= "Test"
                        }
                    }
                });
        }

        private static void AddMappeResultatMinimum(SokeresultatMinimum sokeresultatMinimum)
        {
            sokeresultatMinimum.ResultatListe.Add(
                new ResultatMinimum()
                {
                    Mappe = new MappeMinimum()
                    {
                        SystemID = new SystemID() { Label = "", Value = "02379f7b-b99f-44e6-a711-822f00002587" },
                        MappeID = "Test",
                        Tittel = "Test1",
                    }
                });
            sokeresultatMinimum.ResultatListe.Add(
                new ResultatMinimum()
                {
                    Mappe = new MappeMinimum()
                    {
                        SystemID = new SystemID() { Label = "", Value = "02379f7b-b99f-44e6-a711-822f00002587" },
                        MappeID = "Test",
                        Tittel = "Test2",
                    }
                });
        }

        public static Sokeresultat CreateSokeResultatUtvidet(Sokdefinisjon sokdefinisjon)
        {
            var sokeResultatUtvidet = new Sokeresultat()
            {
                System = "",
                Take = 10,
                Skip = 0,
                Tidspunkt = DateTime.Now
            };

            switch (sokdefinisjon)
            {
                case MappeSokdefinisjon:
                    AddMappeResultatUtvidet(sokeResultatUtvidet);
                    break;
                case SaksmappeSokdefinisjon:
                    AddSaksmappeResultatUtvidet(sokeResultatUtvidet);
                    break;
            }
            return sokeResultatUtvidet;
        }

        private static void AddSaksmappeResultatUtvidet(Sokeresultat sokeResultatUtvidet)
        {
            sokeResultatUtvidet.ResultatListe.Add(
                new Resultat()
                {
                    Saksmappe = new Saksmappe()
                    {
                        SystemID = new SystemID() { Label = "", Value = "02379f7b-b99f-44e6-a711-822f00002587" },
                        MappeID = "Test",
                        Tittel = "Test1",
                        OpprettetDato = DateTime.Now,
                        OpprettetAv = "Test",
                        AvsluttetDato = DateTime.Now,
                        AvsluttetAv = "Test",
                        Saksaar = DateTime.Now.Year,
                        Sakssekvensnummer = 1,
                        Saksdato = DateTime.Now,
                        AdministrativEnhet = new AdministrativEnhet()
                        {
                            Navn = "Test"
                        },
                        Saksansvarlig = new Saksansvarlig()
                            {
                                Navn = "Test"
                            },
                        Saksstatus = new Saksstatus()
                        {
                            KodeProperty= "Test"
                        }
                    }
                });
            
            sokeResultatUtvidet.ResultatListe.Add(
                new Resultat()
                {
                    Saksmappe = new Saksmappe()
                    {
                        SystemID = new SystemID() { Label = "", Value = "02379f7b-b99f-44e6-a711-822f00002587" },
                        MappeID = "Test",
                        Tittel = "Test2",
                        OpprettetDato = DateTime.Now,
                        OpprettetAv = "Test",
                        AvsluttetDato = DateTime.Now,
                        AvsluttetAv = "Test",
                        Saksaar = DateTime.Now.Year,
                        Sakssekvensnummer = 2,
                        Saksdato = DateTime.Now,
                        AdministrativEnhet = new AdministrativEnhet()
                        {
                            Navn = "Test"
                        },
                        Saksansvarlig = new Saksansvarlig()
                        {
                            Navn = "Test"
                        },
                        Saksstatus = new Saksstatus()
                        {
                            KodeProperty= "Test"
                        }
                    }
                });
        }

        private static void AddMappeResultatUtvidet(Sokeresultat sokeResultatUtvidet)
        {
            sokeResultatUtvidet.ResultatListe.Add(
                new Resultat()
                {
                    Mappe = new Mappe()
                    {
                        SystemID = new SystemID() { Label = "", Value = "02379f7b-b99f-44e6-a711-822f00002587" },
                        MappeID = "Test",
                        Tittel = "Test1",
                        OpprettetDato = DateTime.Now,
                        OpprettetAv = "Test",
                        AvsluttetDato = DateTime.Now,
                        AvsluttetAv = "Test",
                    }
                });
            
            sokeResultatUtvidet.ResultatListe.Add(
                new Resultat()
                {
                    Mappe = new Mappe()
                    {
                        SystemID = new SystemID() { Label = "", Value = "02379f7b-b99f-44e6-a711-822f00002587" },
                        MappeID = "Test",
                        Tittel = "Test2",
                        OpprettetDato = DateTime.Now,
                        OpprettetAv = "Test",
                        AvsluttetDato = DateTime.Now,
                        AvsluttetAv = "Test",
                    }
                });
        }

        public static SokeresultatNoekler CreateSokeResultatNoekler()
        {
            var sokeResultatNoekler = new SokeresultatNoekler()
            {
                System = "",
                Take = 10,
                Skip = 0,
                Tidspunkt = DateTime.Now, 
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