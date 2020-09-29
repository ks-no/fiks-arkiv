using System;
using System.Collections.Generic;
using System.Globalization;
using ks.fiks.io.fagsystem.arkiv.sample.ForenkletArkivering;
using no.ks.fiks.io.arkivmelding.sok;
using NUnit.Framework;
using FIKS.eMeldingArkiv.eMeldingForenkletArkiv;

namespace ks.fiks.io.arkivintegrasjon.tests
{
    class UnitTestBrukerhistorie10Oppmalingsdialog
    {
        public void Setup()
        {
        }

        // Skal sjekke om det finnes en sak med angitt saksår og saksseksvensnummer i akrivet
        [Test]
        public void SjekkSakMedSaksnummerFinnes()
        {
            int saksaar = 2020;
            int saksaksekvensnummer = 123;

            var arkivmeldingsok = new sok
            {
                respons = respons_type.saksmappe,
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
                    felt = field_type.saksaksaar,
                    @operator = operator_type.equal,
                    parameterverdier = new parameterverdier
                    {
                        Item = new intvalues
                        {
                            value =new[] {saksaar }

                        }
                    }
                },
                new parameter
                {
                    felt = field_type.saksaksekvensnummer,
                    @operator = operator_type.equal,
                    parameterverdier = new parameterverdier
                    {
                        Item = new intvalues
                        {
                            value =new[] { saksaksekvensnummer }

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
