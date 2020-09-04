using ks.fiks.io.fagsystem.arkiv.sample.ForenkletArkivering;
using no.ks.fiks.io.arkivmelding.sok;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace ks.fiks.io.arkivintegrasjon.tests
{
    public class UnitTestsSok
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestSok()
        {



            var arkivmeldingsok = new sok();
            arkivmeldingsok.respons = respons_type.mappe;
            arkivmeldingsok.meldingId = Guid.NewGuid().ToString();
            arkivmeldingsok.system = "Fagsystem X";
            arkivmeldingsok.tidspunkt = DateTime.Now;
            arkivmeldingsok.skip = 0;
            arkivmeldingsok.take = 100;
            //TODO lage list på parameter i steden for []

            string payload = Arkivintegrasjon.Serialize(arkivmeldingsok);

            Assert.Pass();
        }
    }
}
