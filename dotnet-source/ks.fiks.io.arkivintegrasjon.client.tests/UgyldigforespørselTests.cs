using System;
using ks.fiks.io.arkivintegrasjon.client.ForenkletArkivering;
using System.Text.Json;
using NUnit.Framework;

namespace ks.fiks.io.arkivintegrasjon.client.tests
{
    public class UgyldigforespørselTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void ToString_gir_serialized_json()
        {
            var ugyldigforespørsel = new Ugyldigforespørsel() { errorId = "errorId", feilmelding = "feilmelding"};
            var ugyldigforespørselJson = ugyldigforespørsel.ToString();
            var deserializedMelding = JsonSerializer.Deserialize<Ugyldigforespørsel>(ugyldigforespørselJson);
            Assert.IsNotNull(deserializedMelding);
            Assert.AreEqual(ugyldigforespørsel.errorId, deserializedMelding.errorId);
        }
    }
}