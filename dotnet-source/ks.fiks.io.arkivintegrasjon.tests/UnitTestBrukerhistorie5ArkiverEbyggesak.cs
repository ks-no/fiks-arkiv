using FIKS.eMeldingArkiv.eMeldingForenkletArkiv;
using ks.fiks.io.arkivintegrasjon.sample.messages;
using ks.fiks.io.fagsystem.arkiv.sample.ForenkletArkivering;
using no.ks.fiks.io.arkivmelding;
using no.ks.fiks.io.arkivmelding.sok;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace ks.fiks.io.arkivintegrasjon.tests
{
    class UnitTestBrukerhistorie5ArkiverEbyggesak
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestEbyggesak()
        {
            // Name of system (eksternsystem)
            var ekstsys = "eByggesak";
            // Saksid in eByggesak
            var saksid = "123";

            // Finnes det sak fra før?
            var finnSak = new no.ks.fiks.io.arkivmelding.sok.sok
            {
                respons = respons_type.mappe,
                meldingId = Guid.NewGuid().ToString(),
                system = "eByggesak",
                tidspunkt = DateTime.Now,
                skip = 0,
                take = 2
            };

            var paramlist = new List<parameter>
                {
                    new parameter
                    {
                        felt = field_type.mappeeksternId,
                        @operator = operator_type.equal,
                        parameterverdier = new parameterverdier
                        {
                            Item = new stringvalues {value = new[] {ekstsys, saksid}}
                        }
                    }
                };


            finnSak.parameter = paramlist.ToArray();

            var payload = Arkivintegrasjon.Serialize(finnSak);

            // Check if there was a case
            string systemid = null;

            // Det fantes ikke sak, lag
            if (systemid == null)
            {
                Klasse gnr = new Klasse
                {
                    klasseID = "1234-12/1234",
                    klassifikasjonssystem = "GNR"
                };
                // TODO: Mange manglende felt vs. GI 1.1
                Saksmappe saksmappe = new Saksmappe
                {
                    tittel = "Byggesak 123",
                    offentligTittel = "Byggesak 123",
                    administrativEnhet = "Byggesaksavdelingen",
                    saksansvarlig = "Byggesaksbehandler",
                    saksdato = new DateTime(),
                    saksstatus = "B",
                    // dokumentmedium
                    // arkivdel
                    mappetype = new Kode
                    { kodeverdi = "Saksmappe"},
                    klasse = new List<Klasse> { gnr },
                    // sakspart
                    // merknad
                    // matrikkelnummer
                    // punkt
                    // bevaringstid
                    // kassasjonsvedtak
                    skjermetTittel = true,
                    // skjerming
                    // prosjekt
                    // tilgangsgruppe
                    referanseEksternNoekkel = new EksternNoekkel
                    {
                        fagsystem = ekstsys,
                        noekkel = saksid
                    }
                };
                payload = Arkivintegrasjon.Serialize(saksmappe);

                systemid = "12345"; // Nøkkel fra arkivering av saksmappen / søk
            }

            // Overfør nye journalposter
            // Løkke som går gjennom både I, U og X (og S), eksempler her

            // Inngående
            InnkommendeJournalpost inn = new InnkommendeJournalpost
            {
                // Referanse til sak?
                avsender = new List<Korrespondansepart> {new Korrespondansepart
                {
                    enhetsidentifikator = new Enhetsidentifikator
                    {
                        organisasjonsnummer = "123456789"
                    },
                    navn = "Testesen",
                    postadresse = new EnkelAdresse
                    {
                        adresselinje1 = "Testveien 3",
                        postnr = "1234",
                        poststed = "Poststed"
                    },
                } },
                internMottaker = new List<KorrespondansepartIntern> { new KorrespondansepartIntern
                {
                    saksbehandler = "Bygg Behandler"
                } },
                referanseEksternNoekkel = new EksternNoekkel
                {
                    fagsystem = "eByggesak",
                    noekkel = "inn1"
                },
                tittel = "Søknad om rammetillatelse"
            };
            // Arkivere...

            // Gjenta for utgående

            // Gjenta for notater

            // Gjenta for saksfremlegg

            Assert.Pass();
        }
    }
}
