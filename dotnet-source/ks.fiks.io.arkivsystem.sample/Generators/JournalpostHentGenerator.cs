using System;
using KS.Fiks.Arkiv.Models.V1.Arkivstruktur;
using KS.Fiks.Arkiv.Models.V1.Innsyn.Hent.Journalpost;
using KS.Fiks.Arkiv.Models.V1.Metadatakatalog;

namespace ks.fiks.io.arkivsystem.sample.Generators
{
    public class JournalpostHentGenerator
    {
        private const string DokumentbeskrivelseOpprettetAvDefault = "Foo";
        private const string JournalpostnummerDefault = "1";
        private const string DokumentbeskrivelseDokumentnummerDefault = "1";
        private const string DokumentbeskrivelseTilknyttetAvDefault = "foo";
        private const string DokumentobjektOpprettetAvDefault = "foo";
        private const string DokumentobjektSjekksumDefault = "foo";
        private const string DokumentobjektSjekksumAlgoritmeDefault = "MD5";
        private const string DokumentobjektFilstoerrelseDefault = "100";

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
        
        public static Journalpost CreateHentJournalpostArkivmeldingJournalpost (KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding.Journalpost arkivmeldingJournalpost)
        {
            var jp = new Journalpost()
            {
                OpprettetAv = arkivmeldingJournalpost.OpprettetAv,
                ArkivertAv = arkivmeldingJournalpost.ArkivertAv,
                ReferanseForelderMappe = new SystemID() { Label = arkivmeldingJournalpost.ReferanseForelderMappe.Label, Value = arkivmeldingJournalpost.ReferanseForelderMappe.Value },
                ReferanseEksternNoekkel = new EksternNoekkel()
                {
                    Fagsystem = arkivmeldingJournalpost.ReferanseEksternNoekkel.Fagsystem,
                    Noekkel = arkivmeldingJournalpost.ReferanseEksternNoekkel.Noekkel
                },
                Tittel = arkivmeldingJournalpost.Tittel,
                Journalaar = DateTime.Now.Year.ToString(),
                Journalsekvensnummer = arkivmeldingJournalpost.Journalsekvensnummer ?? DateTime.Now.ToString(), //Setter denne til noe unikt via DateTime.Now
                Journalpostnummer = arkivmeldingJournalpost.Journalpostnummer ?? JournalpostnummerDefault,
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
                // Den må få en SystemID som er påkrevd
                nyDokumentbeskrivelse.SystemID = new SystemID()
                {
                    Value = Guid.NewGuid().ToString()
                };
                
                jp.Dokumentbeskrivelse.Add(nyDokumentbeskrivelse);
            }

            // Den må få en SystemID som er påkrevd
            jp.SystemID = new SystemID()
            {
                Value = Guid.NewGuid().ToString()
            };
            
            return jp;
        }
        
        public static Journalpost CreateJournalpost()
        {
            return new Journalpost()
            {
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
                        Dokumenttype = new Dokumenttype()
                        {
                            KodeProperty= "SØKNAD"
                        },
                        Dokumentstatus = new Dokumentstatus()
                        {
                            KodeProperty= "F"
                        },
                        Tittel = "Rekvisisjon av oppmålingsforretning",
                        TilknyttetRegistreringSom = new TilknyttetRegistreringSom()
                        {
                            KodeProperty= "H"
                        },
                        Dokumentobjekt =
                        {
                            new Dokumentobjekt()
                            {
                                Versjonsnummer = "1",
                                Variantformat = new Variantformat()
                                {
                                    KodeProperty= "P"
                                },
                                Format = new Format()
                                {
                                    KodeProperty= "PDF"
                                },
                                Filnavn = "rekvisjon.pdf",
                                ReferanseDokumentfil = "rekvisisjon.pdf"
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
                        Saksbehandler = "Ingrid Mottaker"
                    }
                },
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