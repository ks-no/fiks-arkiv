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

        public void TestSokFlereFelt()
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

            paramlist.Add(
                new parameter()
                {
                    felt = field_type.mappeOpprettetDato,
                    @operator = operator_type.equal,
                    parameterverdier = new parameterverdier()
                    {
                        Item = new datevalues()
                        {
                                value = new DateTime[] { DateTime.ParseExact("2009-05-08", "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
                            }
                        }
                    }
                });

            arkivmeldingsok.parameter = paramlist.ToArray();

            string payload = Arkivintegrasjon.Serialize(arkivmeldingsok);

            Assert.Pass();
        }


        [Test]
        public void TestSokDato()
        {

            var arkivmeldingsok = new sok
            {
                respons = respons_type.journalpost,
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
                    felt = field_type.journalpostjournaldato,
                    @operator = operator_type.between,
                    parameterverdier = new parameterverdier()
                    {
                        Item = new datevalues()
                        {
                            value = new DateTime[] { DateTime.ParseExact("2009-05-08", "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), 
                                DateTime.ParseExact("2009-05-09", "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
                            }
                        }
                    }
                });

            arkivmeldingsok.parameter = paramlist.ToArray();

            string payload = Arkivintegrasjon.Serialize(arkivmeldingsok);

            Assert.Pass();
        }


        [Test]
        public void TestSokKlassifikasjon()
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
                        Item = new klassifikasjonvalues()
                        {
                            klassifikasjonssystem = new string[1] { "GBNR" },
                            klasse = new string[1] { "21/400" }
                        }
                    }
                }); ;

            arkivmeldingsok.parameter = paramlist.ToArray();

            string payload = Arkivintegrasjon.Serialize(arkivmeldingsok);

            Assert.Pass();
        }


        [Test]
        public void TestSøkVsm()
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
                    felt = field_type.mappevirksomhetsspesifikkeMetadata,
                    @operator = operator_type.equal,
                    parameterverdier = new parameterverdier()
                    {
                        Item = new vsmetadata()
                        {
                            key = new string[1] { "Kaffetype" },
                            value = new string[1] { "arabica" }
                        }
                    }
                }); ;

            arkivmeldingsok.parameter = paramlist.ToArray();

            string payload = Arkivintegrasjon.Serialize(arkivmeldingsok);

            Assert.Pass();
        }
    }
}
