using FIKS.eMeldingArkiv.eMeldingForenkletArkiv;
using ks.fiks.io.arkivintegrasjon.sample.messages;
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

            //var payload = Arkivintegrasjon.Serialize(finnSak);

            // Check if there was a case
            string systemid = null;

            // Det fantes ikke sak, lag
            if (systemid == null)
            {
                klasse gnr = new klasse
                {
                    klasseID = "1234-12/1234",
                    klassifikasjonssystem = "GNR"
                };
                // TODO: Mange manglende felt vs. GI 1.1
                saksmappe saksmappe = new saksmappe
                {
                    tittel = "Byggesak 123",
                    offentligTittel = "Byggesak 123",
                    administrativEnhet = "Byggesaksavdelingen",
                    saksansvarlig = "Byggesaksbehandler",
                    saksdato = new DateTime(),
                    saksstatus = "B",
                    dokumentmedium = "elektronisk", // Kode?
                    // arkivdel = "BYGG", // Mangler og bør være kodeobjekt
                    // mappetype = new Kode
                    // { kodeverdi = "Saksmappe"}, // Standardiseres?
                    klasse = new klasse[] { gnr },
                    part = new part[]
                    {
                        new part
                        {
                            partNavn = "Fr Tiltakshaver"    // navn som for korrespondansepart?
                        }
                    },
                    merknad = new merknad[]
                    {
                        new merknad
                        {
                            merknadstype = "BYGG",  // Kode?
                            merknadstekst = "Saksnummer 20/123 i eByggesak"
                        }
                    },
                    // matrikkelnummer
                    // punkt
                    // bevaringstid
                    // kassasjonsvedtak
                    skjerming = new skjerming
                    {
                        tilgangsrestriksjon = "13", // Settes av server?
                        skjermingshjemmel = "Ofl § 13, fvl § 123",
                        skjermingMetadata = new string[] {"tittel"} // Her må det være kodeverk
                    },
                    // prosjekt
                    // tilgangsgruppe
                    // merknad
                    referanseEksternNoekkel = new eksternNoekkel
                    {
                        fagsystem = ekstsys,
                        noekkel = saksid
                    }
                };
                // payload = Arkivintegrasjon.Serialize(saksmappe);

                systemid = "12345"; // Nøkkel fra arkivering av saksmappen / søk
            }

            // Overfør nye journalposter
            // Løkke som går gjennom både I, U og X (og S), eksempler her

            // Inngående
            journalpost inn = new journalpost   // Beholde objekttyper for inn-, ut- etc.?
            {
                // Referanse til sak?
                korrespondansepart = new korrespondansepart[] {
                    new korrespondansepart
                    {
                        korrespondanseparttype = "avsender",    // Kode?
                        Item = new EnhetsidentifikatorType      // Håpløst feltnavn
                        {
                            organisasjonsnummer = "123456789"
                        },
                        korrespondansepartNavn = "Testesen",
                        postadresse = new string[] { "c/o Hei og hå", "Testveien 3" },
                        postnummer = "1234",
                        poststed = "Poststed",
                    },
                    new korrespondansepart
                    {
                        korrespondanseparttype = "kopimottager",    // Kode?
                        Item = new FoedselsnummerType
                        {
                            foedselsnummer = "12345612345"
                        },
                        korrespondansepartNavn = "Advokat NN",  // Hvordan skille person og organisasjon?
                        postadresse = new string[] { "Krøsusveien 3" },
                        postnummer = "2345",
                        poststed = "Poststedet",
                    },
                    new korrespondansepart
                    {
                        saksbehandler = "SBBYGG",
                        administrativEnhet = "BYGG"
                    }
                },
                referanseEksternNoekkel = new eksternNoekkel
                {
                    fagsystem = "eByggesak",
                    noekkel = "20/1234-12"
                },
                tittel = "Søknad om rammetillatelse 12/123",
                offentligTittel = "Søknad om rammetillatelse 12/123",
                // status
                dokumentetsDato = new DateTime(2020,10,5),
                // merknad
                skjerming = new skjerming
                {
                   tilgangsrestriksjon = "13",
                    skjermingshjemmel = "Off.loven § 13",
                    skjermingsvarighet = "60"   // Antall år bør ikke være string
                }
                // Dokumenter
            };
            // Arkivere...

            // Gjenta for utgående

            // Gjenta for notater

            // Gjenta for saksfremlegg

            Assert.Pass();
        }
    }
}
