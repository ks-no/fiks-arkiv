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

            var arkivmeldingsok = new sok
            {
                respons = respons_type.mappe,
                meldingId = Guid.NewGuid().ToString(),
                system = "Fagsystem X",
                tidspunkt = DateTime.Now,
                skip = 0,
                take = 100
            };

            List<parameter> paramlist = new List<parameter>();
            
            paramlist.Add(
                new parameter()
                {
                    felt = field_type.mappetittel,
                    @operator = operator_type.equal,
                    parameterverdier = new parameterverdier() 
                    {
                        Item = new stringvalues() 
                        {
                            value = new string[1] { "tittel*" } 
                        } 
                    } 
                });

            arkivmeldingsok.parameter = paramlist.ToArray();

            string payload = Arkivintegrasjon.Serialize(arkivmeldingsok);

            Assert.Pass();
        }
    }
}
