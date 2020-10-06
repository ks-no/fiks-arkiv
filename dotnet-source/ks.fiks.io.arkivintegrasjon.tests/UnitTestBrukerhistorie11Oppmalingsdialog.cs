using System;
using System.Collections.Generic;
using System.Globalization;
using ks.fiks.io.fagsystem.arkiv.sample.ForenkletArkivering;
using no.ks.fiks.io.arkivmelding.sok;
using NUnit.Framework;
using FIKS.eMeldingArkiv.eMeldingForenkletArkiv;
using no.ks.fiks.io.arkivmelding;

namespace ks.fiks.io.arkivintegrasjon.tests
{
    class UnitTestBrukerhistorie11Oppmalingsdialog
    {

        public void Setup()
        {
        }

        // fagsystem har dokumentID til dokumentet skal finne dokumentet for visnign i klient
        [Test]
        public void TestFinnDokumentFraId()
        {
            int saksaar = 2020;
            int saksaksekvensnummer = 123;

            var arkivmeldingsok = new sok
            {
                respons = respons_type.dokumentbeskrivelse,
                meldingId = Guid.NewGuid().ToString(),
                system = "Fagsystem X",
                tidspunkt = DateTime.Now,
                skip = 0,
                take = 100
            };
            // må søke på ekstenID finner ikke noe felt for doklument id.

            // finner ikke noe felt for dokument ekstenid.
            List<parameter> paramlist = new List<parameter>
            {
                new parameter
                {
                   // felt = field_type.
                    //@operator = operator_type.equal,
                    //parameterverdier = new parameterverdier
                    //{
                    //    Item = new eksternId
                    //    {
                    //        value =new[] {saksaar }

                    //    }
                    //}
                }

            };

            arkivmeldingsok.parameter = paramlist.ToArray();

            var payload = Arkivintegrasjon.Serialize(arkivmeldingsok);




            Assert.Pass();
        }
    }
}
