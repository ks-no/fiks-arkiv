using KS.Fiks.Arkiv.Models.V1.Innsyn.Sok;
using KS.Fiks.Arkiv.Models.V1.Meldingstyper;
using ks.fiks.io.arkivsystem.sample.Helpers;
using ks.fiks.io.arkivsystem.sample.Models;

namespace ks.fiks.io.arkivsystem.sample.Generators
{
    public class SokGenerator
    {
        public static Melding CreateSokResponseMelding(Sok sok) =>
            sok.ResponsType switch
            {
                ResponsType.Minimum =>
                    new Melding
                    {
                        FileName = "sokeresultat-minimum.xml",
                        MeldingsType = FiksArkivV1Meldingtype.SokResultatMinimum,
                        ResultatMelding = SokeresultatGenerator.CreateSokeResultatMinimum(sok.Respons)
                    },
                ResponsType.Noekler =>
                    new Melding
                    {
                        FileName = "sokeresultat-noekler.xml",
                        MeldingsType = FiksArkivV1Meldingtype.SokResultatNoekler,
                        ResultatMelding = SokeresultatGenerator.CreateSokeResultatNoekler(),
                    },
                ResponsType.Utvidet =>
                    new Melding
                    {
                        FileName = "sokeresultat-utvidet.xml",
                        MeldingsType = FiksArkivV1Meldingtype.SokResultatUtvidet,
                        ResultatMelding = SokeresultatGenerator.CreateSokeResultatUtvidet(sok.Respons)
                    },
                _ =>
                    new Melding
                    {
                        FileName = "sokeresultat-minimum.xml",
                        MeldingsType = FiksArkivV1Meldingtype.SokResultatMinimum,
                        ResultatMelding = SokeresultatGenerator.CreateSokeResultatMinimum(sok.Respons),
                    }
            };

    }
}