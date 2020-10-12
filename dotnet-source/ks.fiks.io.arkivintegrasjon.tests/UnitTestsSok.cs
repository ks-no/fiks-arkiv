using System;
using System.Collections.Generic;
using System.Globalization;
using ks.fiks.io.arkivintegrasjon.sample.messages;
using ks.fiks.io.fagsystem.arkiv.sample.ForenkletArkivering;
using no.ks.fiks.io.arkivmelding.sok;
using NUnit.Framework;

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

            var arkivmeldingsok = MessageSamples.SokTittel("tittel*");
            string payload = Arkivintegrasjon.Serialize(arkivmeldingsok);

            Assert.Pass();
        }

        [Test]
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

            var paramlist = new List<parameter>
            {
                new parameter
                {
                    felt = field_type.mappetittel,
                    @operator = operator_type.equal,
                    parameterverdier = new parameterverdier
                    {
                        Item = new stringvalues {value = new[] {"tittel*"}}
                    }
                },
                new parameter
                {
                    felt = field_type.mappeopprettetDato,
                    @operator = operator_type.equal,
                    parameterverdier = new parameterverdier
                    {
                        Item = new datevalues
                        {
                            value = new[]
                            {
                                DateTime.ParseExact("2009-05-08", "yyyy-MM-dd", CultureInfo.InvariantCulture)
                            }
                        }
                    }
                }
            };



            arkivmeldingsok.parameter = paramlist.ToArray();

            var payload = Arkivintegrasjon.Serialize(arkivmeldingsok);

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

            var paramlist = new List<parameter>
            {
                new parameter
                {
                    felt = field_type.journalpostjournaldato,
                    @operator = operator_type.between,
                    parameterverdier = new parameterverdier
                    {
                        Item = new datevalues
                        {
                            value = new[]
                            {
                                DateTime.ParseExact("2009-05-08", "yyyy-MM-dd",
                                    CultureInfo.InvariantCulture),
                                DateTime.ParseExact("2009-05-09", "yyyy-MM-dd",
                                    CultureInfo.InvariantCulture)
                            }
                        }
                    }
                }
            };


            arkivmeldingsok.parameter = paramlist.ToArray();

            var payload = Arkivintegrasjon.Serialize(arkivmeldingsok);

            Assert.Pass();
        }


        [Test]
        public void TestSokEksternId()
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

            var paramlist = new List<parameter>
            {
                new parameter
                {
                    felt = field_type.registreringeksternId,
                    @operator = operator_type.equal,
                    parameterverdier = new parameterverdier
                    {
                        Item = new eksternId
                        {
                            system = "SikriElements",
                            id = "85295a5e-6415-410c-8a2c-5b368f1ed06c"
                        }
                    }
                }
            };


            arkivmeldingsok.parameter = paramlist.ToArray();

            var payload = Arkivintegrasjon.Serialize(arkivmeldingsok);

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

            List<parameter> paramlist = new List<parameter>
            {
                new parameter
                {
                    felt = field_type.mappetittel,
                    @operator = operator_type.equal,
                    parameterverdier = new parameterverdier
                    {
                        Item = new klassifikasjonvalues
                        {
                            klassifikasjonssystem = new[] {"GBNR"},
                            klasse = new[] {"21/400"}
                        }
                    }
                }
            };

            arkivmeldingsok.parameter = paramlist.ToArray();

            var payload = Arkivintegrasjon.Serialize(arkivmeldingsok);

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

            List<parameter> paramlist = new List<parameter>
            {
                new parameter
                {
                    felt = field_type.mappevirksomhetsspesifikkeMetadata,
                    @operator = operator_type.equal,
                    parameterverdier = new parameterverdier
                    {
                        Item = new vsmetadata
                        {
                            key = new[] {"Kaffetype"}, value = new[] {"arabica"}
                        }
                    }
                }
            };

            arkivmeldingsok.parameter = paramlist.ToArray();

            var payload = Arkivintegrasjon.Serialize(arkivmeldingsok);

            Assert.Pass();
        }
    }
}
