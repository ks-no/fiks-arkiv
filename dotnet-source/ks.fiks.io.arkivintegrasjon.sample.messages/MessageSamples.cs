using FIKS.eMeldingArkiv.eMeldingForenkletArkiv;
using no.ks.fiks.io.arkivmelding.sok;
using System;
using System.Collections.Generic;

namespace ks.fiks.io.arkivintegrasjon.sample.messages
{
    /// <summary>
    /// Felles bibliotek for å hente ut predefinert meldinger
    /// </summary>
    public class MessageSamples
    {
        public static ArkivmeldingForenkletInnkommende GetForenkletArkivmeldingInngåendeMedSaksreferanse()
        {
            //Fagsystem definerer ønsket struktur
            ArkivmeldingForenkletInnkommende inng = new ArkivmeldingForenkletInnkommende();
            inng.sluttbrukerIdentifikator = "Fagsystemets brukerid";

            inng.referanseSaksmappe = new Saksmappe()
            {
                saksaar = 2018,
                sakssekvensnummer = 123456
            };

            inng.nyInnkommendeJournalpost = new InnkommendeJournalpost
            {
                tittel = "Tittel journalpost",
                mottattDato = DateTime.Today,
                dokumentetsDato = DateTime.Today.AddDays(-2),
                offentlighetsvurdertDato = DateTime.Today,
            };

            inng.nyInnkommendeJournalpost.referanseEksternNøkkel = new EksternNøkkel
            {
                fagsystem = "Fagsystem X",
                nøkkel = "e4712424-883c-4068-9cb7-97ac679d7232"
            };

            inng.nyInnkommendeJournalpost.internMottaker = new List<KorrespondansepartIntern>
            {
                new KorrespondansepartIntern() {
                    administrativEnhet = "Oppmålingsetaten",
                    referanseAdministrativEnhet = "b631f24b-48fb-4b5c-838e-6a1f7d56fae2"
                }
            };

            inng.nyInnkommendeJournalpost.mottaker = new List<Korrespondansepart>
            {
                new Korrespondansepart() {
                    navn = "Test kommune",
                    enhetsidentifikator = new Enhetsidentifikator() {
                        organisasjonsnummer = "123456789"
                    },
                    postadresse = new EnkelAdresse() {
                        adresselinje1 = "Oppmålingsetaten",
                        adresselinje2 = "Rådhusgate 1",
                        postnr = "3801",
                        poststed = "Bø"
                    }
                }
            };


            inng.nyInnkommendeJournalpost.avsender = new List<Korrespondansepart>
            {
                new Korrespondansepart() {
                    navn = "Anita Avsender",
                    personid = new Personidentifikator() { personidentifikatorType = "F",  personidentifikatorNr = "12345678901"},
                    postadresse = new EnkelAdresse() {
                        adresselinje1 = "Gate 1",
                        postnr = "3801",
                        poststed = "Bø" }
                }
            };


            inng.nyInnkommendeJournalpost.hoveddokument = new ForenkletDokument
            {
                tittel = "Rekvisisjon av oppmålingsforretning",
                filnavn = "rekvisisjon.pdf"
            };

            inng.nyInnkommendeJournalpost.vedlegg = new List<ForenkletDokument>
            {
                new ForenkletDokument(){
                    tittel = "Vedlegg 1",
                    filnavn = "vedlegg.pdf"
                }
            };
           
            return inng;
        }

        public static OppdaterSaksmappe GetOppdaterSaksmappeAnsvarligPåFagsystemnøkkel(string fagsystem, string nøkkel, string saksansvarlig, string idSaksansvarlig)
        {
            var saksmappe = new OppdaterSaksmappe
            {
                oppdaterSaksmappe = new Saksmappe()
                {
                    saksansvarlig = saksansvarlig,
                    referanseSaksansvarlig = idSaksansvarlig,
                    referanseEksternNøkkel = new EksternNøkkel
                    {
                        fagsystem = fagsystem,
                        nøkkel = nøkkel
                    }
                }
            };
            
            
            return saksmappe;
        }

        public static OppdaterSaksmappe GetOppdaterSaksmappeAnsvarligPåSaksnummer(int saksaar, int sekvensnr, string saksansvarlig, string idSaksansvarlig)
        {
            var saksmappe = new OppdaterSaksmappe();

            saksmappe.oppdaterSaksmappe = new Saksmappe
            {
                saksaar = saksaar,
                sakssekvensnummer = sekvensnr,
                saksansvarlig = saksansvarlig,
                referanseSaksansvarlig = idSaksansvarlig
            };

            return saksmappe;
        }

        public static sok SokTittel(string tittel)
        {
            var arkivmeldingsok = new sok
            {
                respons = respons_type.mappe,
                meldingId = Guid.NewGuid().ToString(),
                system = "Fagsystem X",
                tidspunkt = DateTime.Now,
                skip = 0,
                take = 100
            };

            var paramlist = new List<parameter>
            {
                new parameter
                {
                    felt = field_type.mappetittel,
                    @operator = operator_type.equal,
                    parameterverdier = new parameterverdier
                    {
                        Item = new stringvalues {value = new[] {tittel}}
                    }
                }
            };


            arkivmeldingsok.parameter = paramlist.ToArray();

            return arkivmeldingsok;

        }

        public static string GetOppdaterJournalpostAnsvarlig() {



            return "";
        }

    }
}
