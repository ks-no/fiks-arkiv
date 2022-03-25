using System.IO;
using System.Xml.Schema;
using KS.Fiks.IO.Arkiv.Client.Models;
using KS.Fiks.IO.Arkiv.Client.Models.Innsyn.Sok;
using ks.fiks.io.arkivsystem.sample.Handlers;
using ks.fiks.io.arkivsystem.sample.Helpers;
using ks.fiks.io.arkivsystem.sample.Models;
using KS.Fiks.IO.Client.Models;
using KS.Fiks.IO.Client.Models.Feilmelding;

namespace ks.fiks.io.arkivsystem.sample.Generators
{
    public class SokGenerator
    {
        public static Melding CreateSokResponseMelding(MottattMeldingArgs mottatt)
        {
            var sokXmlSchemaSet = new XmlSchemaSet();
            sokXmlSchemaSet.Add("http://www.ks.no/standarder/fiks/arkiv/sok/v1", Path.Combine("Schema", "sok.xsd"));

            var sok = SokHandler.GetPayload(mottatt, sokXmlSchemaSet, out var xmlValidationErrorOccured,
                out var validationResult);

            if (xmlValidationErrorOccured)
            {
                return new Melding
                {
                    ResultatMelding = FeilmeldingGenerator.CreateUgyldigforespoerselMelding(validationResult),
                    FileName = "payload.json",
                    MeldingsType = FeilmeldingMeldingTypeV1.Ugyldigforespørsel,
                };
            }

            return CreateSokResponseMelding(sok);
        }

        private static Melding CreateSokResponseMelding(Sok sok) =>
            sok.ResponsType switch
            {
                ResponsType.Minimum =>
                    new Melding
                    {
                        FileName = "sokeresultat-minimum.xml",
                        MeldingsType = ArkivintegrasjonMeldingTypeV1.SokResultatMinimum,
                        ResultatMelding = SokeresultatGenerator.CreateSokeResultatMinimum(sok.Respons)
                    },
                ResponsType.Noekler =>
                    new Melding
                    {
                        FileName = "sokeresultat-noekler.xml",
                        MeldingsType = ArkivintegrasjonMeldingTypeV1.SokResultatNoekler,
                        ResultatMelding = SokeresultatGenerator.CreateSokeResultatNoekler(),
                    },
                ResponsType.Utvidet =>
                    new Melding
                    {
                        FileName = "sokeresultat-utvidet.xml",
                        MeldingsType = ArkivintegrasjonMeldingTypeV1.SokResultatUtvidet,
                        ResultatMelding = SokeresultatGenerator.CreateSokeResultatUtvidet(sok.Respons)
                    },
                _ =>
                    new Melding
                    {
                        FileName = "sokeresultat-minimum.xml",
                        MeldingsType = ArkivintegrasjonMeldingTypeV1.SokResultatMinimum,
                        ResultatMelding = SokeresultatGenerator.CreateSokeResultatMinimum(sok.Respons),
                    }
            };

    }
}