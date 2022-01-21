using System;
using System.Collections.ObjectModel;
using KS.Fiks.IO.Arkiv.Client.Models.Arkivstruktur;
using KS.Fiks.IO.Arkiv.Client.Models.Innsyn.Sok;
using KS.Fiks.IO.Arkiv.Client.Models.Metadatakatalog;
using Microsoft.AspNetCore.Routing;
using Microsoft.VisualBasic;

namespace ks.fiks.io.arkivsystem.sample.Helpers
{
    public class SokeresultatGenerator
    {
        public static SokeresultatMinimum CreateSokeResultatMinimum(Respons sokRespons)
        {
            var sokeResultatMinimum = new SokeresultatMinimum()
            {
                System = "",
                MeldingId = "",
                SvarPaMeldingId = "",
                Take = 10,
                Skip = 0,
                Tidspunkt = DateTime.Now,
                
            };
            
            switch (sokRespons)
            {
                case Respons.Mappe:
                    AddMappeResultatMinimum(sokeResultatMinimum);
                    break;
                case Respons.Saksmappe:
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
                    Mappe = new SaksmappeMinimum()
                    {
                        SystemID = new SystemID() { Label = "", Value = "02379f7b-b99f-44e6-a711-822f00002587" },
                        MappeID = "Test",
                        Tittel = "Test1",
                        OpprettetDato = DateTime.Now,
                        OpprettetAv = "Test",
                        AvsluttetDato = DateTime.Now,
                        AvsluttetAv = "Test",
                        Saksdato = DateTime.Now
                    }
                });
            sokeresultatMinimum.ResultatListe.Add(
                new ResultatMinimum()
                {
                    Mappe = new SaksmappeMinimum()
                    {
                        SystemID = new SystemID() { Label = "", Value = "02379f7b-b99f-44e6-a711-822f00002587" },
                        MappeID = "Test",
                        Tittel = "Test2",
                        OpprettetDato = DateTime.Now,
                        OpprettetAv = "Test",
                        AvsluttetDato = DateTime.Now,
                        AvsluttetAv = "Test",
                        Saksdato = DateTime.Now
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
                        OpprettetDato = DateTime.Now,
                        OpprettetAv = "Test",
                        AvsluttetDato = DateTime.Now,
                        AvsluttetAv = "Test",
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
                        OpprettetDato = DateTime.Now,
                        OpprettetAv = "Test",
                        AvsluttetDato = DateTime.Now,
                        AvsluttetAv = "Test",
                    }
                });
        }

        public static Sokeresultat CreateSokeResultatUtvidet()
        {
            var sokeResultatUtvidet = new Sokeresultat()
            {
                System = "",
                MeldingId = "",
                SvarPaMeldingId = "",
                Take = 10,
                Skip = 0,
                Tidspunkt = DateTime.Now, 
                ResultatListe =
                {
                    new Resultat()
                    {
                        Mappe = new Mappe()
                        {
                            SystemID = new SystemID(){Label = "", Value = "02379f7b-b99f-44e6-a711-822f00002587"},
                            MappeID = "Test",
                            Tittel = "Test1",
                            OpprettetDato = DateTime.Now,
                            OpprettetAv = "Test",
                            AvsluttetDato = DateTime.Now,
                            AvsluttetAv = "Test",
                        }
                    },
                    new Resultat()
                    {
                        Mappe = new Mappe()
                        {
                            SystemID = new SystemID(){Label = "", Value = "02379f7b-b99f-44e6-a711-822f00002587"},
                            MappeID = "Test",
                            Tittel = "Test2",
                            OpprettetDato = DateTime.Now,
                            OpprettetAv = "Test",
                            AvsluttetDato = DateTime.Now,
                            AvsluttetAv = "Test",
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
                System = "",
                MeldingId = "",
                SvarPaMeldingId = "",
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