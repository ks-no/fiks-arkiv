using System;
using KS.Fiks.IO.Arkiv.Client.Models.Arkivstruktur;
using KS.Fiks.IO.Arkiv.Client.Models.Innsyn.Sok;
using KS.Fiks.IO.Arkiv.Client.Models.Metadatakatalog;

namespace ks.fiks.io.arkivsystem.sample.Helpers
{
    public class SokeresultatGenerator
    {
        public static SokeresultatMinimum CreateSokeResultatMinimum()
        {
            var sokeResultatMinimum = new SokeresultatMinimum()
            {
                System = "",
                MeldingId = "",
                SvarPaMeldingId = "",
                Take = 10,
                Skip = 0,
                Tidspunkt = DateTime.Now,
                ResultatListe =
                {
                    new ResultatMinimum()
                    {
                        Mappe = new MappeMinimum()
                        {
                            SystemID = new SystemID() {Label = "", Value = "02379f7b-b99f-44e6-a711-822f00002587"},
                            MappeID = "Test",
                            Tittel = "Test1",
                            OpprettetDato = DateTime.Now,
                            OpprettetAv = "Test",
                            AvsluttetDato = DateTime.Now,
                            AvsluttetAv = "Test",
                        }
                    },
                    new ResultatMinimum()
                    {
                        Mappe = new MappeMinimum()
                        {
                            SystemID = new SystemID() {Label = "", Value = "02379f7b-b99f-44e6-a711-822f00002587"},
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
            return sokeResultatMinimum;
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