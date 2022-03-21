using System;
using KS.Fiks.IO.Arkiv.Client.Models.Arkivering.Arkivmelding;
using KS.Fiks.IO.Arkiv.Client.Models.Metadatakatalog;

namespace ks.fiks.io.arkivsystem.sample.Generators
{
    public class ArkivmeldingDataGenerator
    {
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
                        Dokumenttype = "SØKNAD",
                        Dokumentstatus = "F",
                        Tittel = "Rekvisisjon av oppmålingsforretning",
                        TilknyttetRegistreringSom = "H",
                        Dokumentobjekt =
                        {
                            new Dokumentobjekt()
                            {
                                Versjonsnummer = "1",
                                Variantformat = "P",
                                Format = "PDF",
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
                        Korrespondanseparttype = "IM",
                        KorrespondansepartNavn = "Oppmålingsetaten",
                        AdministrativEnhet = "Oppmålingsetaten",
                        Saksbehandler = "Ingrid Mottaker"
                    }
                },
                Journalposttype = "X",
                Journalstatus = "F",
                DokumentetsDato = DateTime.Now.Date,
                MottattDato = DateTime.Now,
            };
        }
    }
}