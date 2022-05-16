using System;
using KS.Fiks.Arkiv.Models.V1.Arkivstruktur;
using KS.Fiks.Arkiv.Models.V1.Innsyn.Hent.Journalpost;
using KS.Fiks.Arkiv.Models.V1.Kodelister;
using KS.Fiks.Arkiv.Models.V1.Metadatakatalog;
using Kode = KS.Fiks.Arkiv.Models.V1.Metadatakatalog.Kode;

namespace ks.fiks.io.arkivsystem.sample.Generators
{
    public class JournalpostHentResultatGenerator
    {
        private const string DokumentbeskrivelseOpprettetAvDefault = "Foo";
        private const string JournalpostnummerDefault = "1";
        private const string JournalsekvensnummerDefault = "1";
        private const string DokumentbeskrivelseDokumentnummerDefault = "1";
        private const string DokumentbeskrivelseTilknyttetAvDefault = "foo";
        private const string DokumentobjektOpprettetAvDefault = "foo";
        private const string DokumentobjektSjekksumDefault = "foo";
        private const string DokumentobjektSjekksumAlgoritmeDefault = "MD5";
        private const string DokumentobjektFilstoerrelseDefault = "100";
        private const string SaksbehandlerKorrespondansepartDefault = "Ingrid Mottaker";

        public static JournalpostHentResultat Create(JournalpostHent journalpostHent)
        {
            var journalpost = CreateJournalpost();
            return Create(journalpostHent, journalpost);
        }
        
        public static JournalpostHentResultat Create(JournalpostHent journalpostHent, Journalpost journalpost)
        {
            if (journalpostHent.ReferanseEksternNoekkel != null)
            {
                journalpost.ReferanseEksternNoekkel = new EksternNoekkel()
                {   
                    Fagsystem = journalpostHent.ReferanseEksternNoekkel.Fagsystem,
                    Noekkel = journalpostHent.ReferanseEksternNoekkel.Noekkel
                };
            } else if (journalpostHent.SystemID != null)
            {
                journalpost.SystemID = journalpostHent.SystemID;    
            }

            return new JournalpostHentResultat()
            {
                Journalpost = journalpost
            };
        }
        
        public static Journalpost CreateHentJournalpostFraArkivmeldingJournalpost (KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding.Journalpost arkivmeldingJournalpost)
        {
            var jp = new Journalpost()
            {
                SystemID = arkivmeldingJournalpost.SystemID ?? new SystemID() { Value = Guid.NewGuid().ToString() }, 
                OpprettetAv = arkivmeldingJournalpost.OpprettetAv,
                ArkivertAv = arkivmeldingJournalpost.ArkivertAv,
                ReferanseEksternNoekkel = new EksternNoekkel()
                {
                    Fagsystem = arkivmeldingJournalpost.ReferanseEksternNoekkel.Fagsystem,
                    Noekkel = arkivmeldingJournalpost.ReferanseEksternNoekkel.Noekkel
                },
                Tittel = arkivmeldingJournalpost.Tittel,
                Journalaar = DateTime.Now.Year.ToString(),
                Journalsekvensnummer = arkivmeldingJournalpost.Journalsekvensnummer ?? JournalsekvensnummerDefault,
                Journalpostnummer = arkivmeldingJournalpost.Journalpostnummer ?? DateTime.Now.Year + DateTime.Now.Millisecond.ToString(),
                Journalposttype = new Journalposttype()
                {
                    KodeProperty = arkivmeldingJournalpost.Journalposttype.KodeProperty
                },
                Journalstatus = new Journalstatus()
                {
                    KodeProperty = arkivmeldingJournalpost.Journalstatus.KodeProperty
                },
                Journaldato = arkivmeldingJournalpost.Journaldato,
                DokumentetsDato = DateTime.Now.Date,
                MottattDato = DateTime.Now,
            };

            if (arkivmeldingJournalpost.ReferanseForelderMappe != null)
            {
                jp.ReferanseForelderMappe = new SystemID()
                {
                    Label = arkivmeldingJournalpost.ReferanseForelderMappe.Label,
                    Value = arkivmeldingJournalpost.ReferanseForelderMappe.Value
                };
            }

            if (arkivmeldingJournalpost.Arkivdel != null)
            {
                jp.Arkivdel = new Kode()
                {
                    Beskrivelse = arkivmeldingJournalpost.Arkivdel.Beskrivelse,
                    KodeProperty = arkivmeldingJournalpost.Arkivdel.KodeProperty
                };
            }

            foreach (var korrespondansepart in arkivmeldingJournalpost.Korrespondansepart)
            {
                jp.Korrespondansepart.Add(new Korrespondansepart()
                {
                    Korrespondanseparttype = new Korrespondanseparttype()
                    {
                        KodeProperty = korrespondansepart.Korrespondanseparttype.KodeProperty
                    },
                    KorrespondansepartNavn = korrespondansepart.KorrespondansepartNavn,
                    AdministrativEnhet = korrespondansepart.AdministrativEnhet,
                    Saksbehandler = korrespondansepart.Saksbehandler
                });
            }

            foreach (var dokumentbeskrivelse in arkivmeldingJournalpost.Dokumentbeskrivelse)
            {
                var nyDokumentbeskrivelse = new Dokumentbeskrivelse()
                {
                    SystemID = dokumentbeskrivelse.SystemID ?? new SystemID() { Value = Guid.NewGuid().ToString() },
                    Dokumenttype = new Dokumenttype()
                    {
                        KodeProperty = dokumentbeskrivelse.Dokumenttype.KodeProperty

                    },
                    Dokumentstatus = new Dokumentstatus()
                    {
                        KodeProperty = dokumentbeskrivelse.Dokumentstatus.KodeProperty
                    },
                    Tittel = dokumentbeskrivelse.Tittel,
                    OpprettetDato = dokumentbeskrivelse.OpprettetDato,
                    OpprettetAv = dokumentbeskrivelse.OpprettetAv ?? DokumentbeskrivelseOpprettetAvDefault,
                    TilknyttetRegistreringSom = new TilknyttetRegistreringSom()
                    {
                        KodeProperty = dokumentbeskrivelse.TilknyttetRegistreringSom.KodeProperty
                    },
                    Dokumentnummer = dokumentbeskrivelse.Dokumentnummer ?? DokumentbeskrivelseDokumentnummerDefault,
                    TilknyttetDato = dokumentbeskrivelse.TilknyttetDato,
                    TilknyttetAv = dokumentbeskrivelse.TilknyttetAv ?? DokumentbeskrivelseTilknyttetAvDefault
                };
                
                //TODO legg til part, merknad fra "lagret" journalpost
                
                foreach (var dokumentobjekt in dokumentbeskrivelse.Dokumentobjekt)
                {
                    nyDokumentbeskrivelse.Dokumentobjekt.Add(new Dokumentobjekt()
                    {
                        SystemID = dokumentobjekt.SystemID ?? new SystemID() {Value = Guid.NewGuid().ToString()},
                        Versjonsnummer = dokumentobjekt.Versjonsnummer,
                        Variantformat = new Variantformat()
                        {
                            KodeProperty = dokumentobjekt.Variantformat.KodeProperty
                        },
                        Format = new Format()
                        {
                            KodeProperty = dokumentobjekt.Format.KodeProperty
                        },
                        Filnavn = dokumentobjekt.Filnavn,
                        OpprettetDato = dokumentobjekt.OpprettetDato,
                        OpprettetAv = dokumentobjekt.OpprettetAv ?? DokumentobjektOpprettetAvDefault,
                        ReferanseDokumentfil = dokumentobjekt.ReferanseDokumentfil,
                        Sjekksum = dokumentobjekt.Sjekksum ?? DokumentobjektSjekksumDefault,
                        SjekksumAlgoritme = dokumentobjekt.SjekksumAlgoritme ?? DokumentobjektSjekksumAlgoritmeDefault,
                        Filstoerrelse = dokumentobjekt.Filstoerrelse ?? DokumentobjektFilstoerrelseDefault
                    });
                }
                jp.Dokumentbeskrivelse.Add(nyDokumentbeskrivelse);
            }
            return jp;
        }

        public static Journalpost CreateJournalpost()
        {
            return new Journalpost()
            {
                SystemID = new SystemID() { Value = Guid.NewGuid().ToString() },
                OpprettetAv = "En brukerid",
                ArkivertAv = "En brukerid",
                ReferanseForelderMappe = new SystemID() { Label = "", Value = Guid.NewGuid().ToString() },
                ReferanseEksternNoekkel = new EksternNoekkel()
                {
                    Fagsystem = "Fagsystem X",
                    Noekkel = Guid.NewGuid().ToString()
                },
                Dokumentbeskrivelse =
                {
                    new Dokumentbeskrivelse()
                    {
                        SystemID = new SystemID() { Value = Guid.NewGuid().ToString() },
                        Dokumenttype = new Dokumenttype()
                        {
                            KodeProperty= "SØKNAD"
                        },
                        Dokumentstatus = new Dokumentstatus()
                        {
                            KodeProperty= "F"
                        },
                        Dokumentnummer = "1",
                        TilknyttetDato = new DateTime(),
                        TilknyttetAv = DokumentbeskrivelseTilknyttetAvDefault,
                        Tittel = "Rekvisisjon av oppmålingsforretning",
                        TilknyttetRegistreringSom = new TilknyttetRegistreringSom()
                        {
                            KodeProperty= "H"
                        },
                        OpprettetDato = DateTime.Now,
                        OpprettetAv = DokumentbeskrivelseOpprettetAvDefault,
                        Dokumentobjekt =
                        {
                            new Dokumentobjekt()
                            {
                                SystemID = new SystemID() { Value = Guid.NewGuid().ToString() },
                                Versjonsnummer = "1",
                                Variantformat = new Variantformat()
                                {
                                    KodeProperty = VariantformatKoder.Arkivformat.Verdi
                                },
                                Format = new Format()
                                {
                                    KodeProperty = FormatKoder.PDFA.Verdi
                                },
                                Filnavn = "Test.pdf",
                                OpprettetDato = DateTime.Now,
                                OpprettetAv = DokumentobjektOpprettetAvDefault,
                                ReferanseDokumentfil = "test.pdf",
                                Sjekksum = DokumentobjektSjekksumDefault,
                                SjekksumAlgoritme = DokumentobjektSjekksumAlgoritmeDefault,
                                Filstoerrelse = DokumentobjektFilstoerrelseDefault
                            }
                        }
                    }
                },
                Tittel = "Internt notat",
                Korrespondansepart =
                {
                    new Korrespondansepart()
                    {
                        Korrespondanseparttype = new Korrespondanseparttype()
                        {
                            KodeProperty= "IM"
                        },
                        KorrespondansepartNavn = "Oppmålingsetaten",
                        AdministrativEnhet = "Oppmålingsetaten",
                        Saksbehandler = SaksbehandlerKorrespondansepartDefault
                    }
                },
                Journalaar = DateTime.Now.Year.ToString(),
                Journalsekvensnummer = "1",
                Journalpostnummer = DateTime.Now.Year + DateTime.Now.Millisecond.ToString(),
                Journalposttype = new Journalposttype()
                {
                    KodeProperty= "X"
                },
                Journalstatus = new Journalstatus()
                {
                    KodeProperty= "F"
                },
                DokumentetsDato = DateTime.Now.Date,
                MottattDato = DateTime.Now,
            };
        }
    }
}