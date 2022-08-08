using System;
using System.Collections;
using System.Collections.Generic;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;
using KS.Fiks.Arkiv.Models.V1.Arkivstruktur;
using KS.Fiks.Arkiv.Models.V1.Arkivstruktur.Minimum;
using KS.Fiks.Arkiv.Models.V1.Arkivstruktur.Noekler;
using KS.Fiks.Arkiv.Models.V1.Innsyn.Sok;
using KS.Fiks.Arkiv.Models.V1.Metadatakatalog;
using EksternNoekkel = KS.Fiks.Arkiv.Models.V1.Arkivstruktur.EksternNoekkel;
using Journalpost = KS.Fiks.Arkiv.Models.V1.Arkivstruktur.Journalpost;
using Mappe = KS.Fiks.Arkiv.Models.V1.Arkivstruktur.Mappe;
using Saksmappe = KS.Fiks.Arkiv.Models.V1.Arkivstruktur.Saksmappe;

namespace ks.fiks.io.arkivsystem.sample.Helpers
{
    public class SokeresultatGenerator
    {
        public SokeresultatMinimum CreateSokeResultatMinimum(Sok sok)
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
            
            switch (sok.Respons)
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

        private void AddSaksmappeResultatMinimum(SokeresultatMinimum sokeresultatMinimum)
        {
            sokeresultatMinimum.ResultatListe.Add(
                new ResultatMinimum()
                {
                    Saksmappe = new SaksmappeMinimum()
                    {
                        SystemID = new SystemID() { Label = "", Value = "02379f7b-b99f-44e6-a711-822f00002587" },
                        MappeID = "Test",
                        Tittel = "Test1",
                        Saksaar = DateTime.Now.Year.ToString(),
                        Sakssekvensnummer = "1",
                        Saksdato = DateTime.Now,
                        AdministrativEnhet = "Test",
                        Saksansvarlig = "Test",
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
                        Saksaar = DateTime.Now.Year.ToString(),
                        Sakssekvensnummer = "1",
                        Saksdato = DateTime.Now,
                        AdministrativEnhet = "Test",
                        Saksansvarlig = "Test",
                        Saksstatus = new Saksstatus()
                        {
                            KodeProperty= "Test"
                        }
                    }
                });
        }

        private void AddMappeResultatMinimum(SokeresultatMinimum sokeresultatMinimum)
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

        public Sokeresultat CreateSokeResultatUtvidet(Sok sok, List<Arkivmelding> lagretArkivmeldinger)
        {
            var sokeResultatUtvidet = new Sokeresultat()
            {
                System = "",
                MeldingId = "",
                SvarPaMeldingId = "",
                Take = 10,
                Skip = 0,
                Tidspunkt = DateTime.Now
            };
            
            switch (sok.Respons)
            {
                case Respons.Mappe:
                    AddMappeResultatUtvidet(sokeResultatUtvidet, lagretArkivmeldinger);
                    break;
                case Respons.Saksmappe:
                    AddSaksmappeResultatUtvidet(sokeResultatUtvidet, lagretArkivmeldinger);
                    break;
                case Respons.Journalpost:
                    AddJournalpostResultatUtvidet(sok, sokeResultatUtvidet, lagretArkivmeldinger);
                    break;
            }
            return sokeResultatUtvidet;
        }

        private void AddJournalpostResultatUtvidet(Sok sok, Sokeresultat sokeResultatUtvidet,
            List<Arkivmelding> lagretArkivmeldinger)
        {
            var count = 0;
            if (lagretArkivmeldinger != null && lagretArkivmeldinger.Count > 0)
            {
                foreach (var parameter in sok.Parameter)
                {
                    if (parameter.Felt == SokFelt.RegistreringTittel)
                    {
                        foreach (var lagretArkivmelding in lagretArkivmeldinger)
                        {
                            foreach (var registrering in lagretArkivmelding.Registrering)
                            {
                                if (parameter.Operator == OperatorType.Equal)
                                {
                                    if (registrering.Tittel.ToLower().Contains(parameter.Parameterverdier.Stringvalues[0].ToLower().Replace("*", string.Empty)))
                                    {
                                        // Funnet!
                                        Console.Out.WriteLineAsync("FUNNET!!!!!!!!!!!!!!!!!!!!");
                                        count++;
                                        var jp = (KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding.Journalpost) registrering;
                                        sokeResultatUtvidet.ResultatListe.Add(new Resultat()
                                        {
                                            Journalpost = new Journalpost()
                                            {
                                                Arkivdel = jp.Arkivdel,
                                                Tittel = jp.Tittel,
                                                OpprettetDato = jp.OpprettetDato,
                                                OpprettetAv = jp.OpprettetAv,
                                                SystemID = jp.SystemID,
                                                ArkivertAv = jp.ArkivertAv,
                                                ArkivertDato = jp.ArkivertDato,
                                                AntallVedlegg = jp.AntallVedlegg,
                                                Journalstatus = new Journalstatus() { Beskrivelse = "", KodeProperty = ""},
                                                Journalposttype = new Journalposttype() {Beskrivelse = "", KodeProperty = ""},
                                                Journalpostnummer = jp.Journalpostnummer ?? "1",
                                                Journalaar = jp.Journalaar ?? DateTime.Now.Year.ToString(),
                                                Journalsekvensnummer = jp.Journalsekvensnummer ?? "1",
                                                Journaldato = jp.Journaldato,
                                                ReferanseEksternNoekkel = new EksternNoekkel()
                                                {
                                                    Fagsystem = jp.ReferanseEksternNoekkel.Fagsystem,
                                                    Noekkel = jp.ReferanseEksternNoekkel.Noekkel
                                                }
                                            }
                                        });
                                    }
                                }
                            }
                        }
                    }
                }
            }

            sokeResultatUtvidet.Count = count;
        }
        

        private void AddSaksmappeResultatUtvidet(Sokeresultat sokeResultatUtvidet,
            List<Arkivmelding> lagretArkivmeldinger)
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
                        Saksaar = DateTime.Now.Year.ToString(),
                        Sakssekvensnummer = "1",
                        Saksdato = DateTime.Now,
                        AdministrativEnhet = "Test",
                        Saksansvarlig = "Test",
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
                        Saksaar = DateTime.Now.Year.ToString(),
                        Sakssekvensnummer = "2",
                        Saksdato = DateTime.Now,
                        AdministrativEnhet = "Test",
                        Saksansvarlig = "Test",
                        Saksstatus = new Saksstatus()
                        {
                            KodeProperty= "Test"
                        }
                    }
                });
        }

        private void AddMappeResultatUtvidet(Sokeresultat sokeResultatUtvidet, List<Arkivmelding> lagretArkivmeldinger)
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

        public SokeresultatNoekler CreateSokeResultatNoekler()
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