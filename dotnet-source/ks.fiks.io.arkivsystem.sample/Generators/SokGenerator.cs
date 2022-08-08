using System.Collections.Generic;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;
using KS.Fiks.Arkiv.Models.V1.Arkivstruktur;
using KS.Fiks.Arkiv.Models.V1.Innsyn.Sok;
using KS.Fiks.Arkiv.Models.V1.Meldingstyper;
using ks.fiks.io.arkivsystem.sample.Helpers;
using ks.fiks.io.arkivsystem.sample.Models;

namespace ks.fiks.io.arkivsystem.sample.Generators
{
    public class SokGenerator
    {

        private SokeresultatGenerator _sokeresultatGenerator; 
        public SokGenerator(SokeresultatGenerator sokeresultatGenerator)
        {
            _sokeresultatGenerator = sokeresultatGenerator;
        }
        
        public List<Melding> CreateSokResponseMelding(Sok sok, List<Arkivmelding> lagretArkivmeldinger)
        {
            var meldinger = new List<Melding>();

            switch (sok.ResponsType)
            {
                case ResponsType.Minimum:
                    meldinger.Add(new Melding
                    {
                        FileName = "resultat.xml",
                        MeldingsType = FiksArkivMeldingtype.SokResultatMinimum,
                        ResultatMelding = _sokeresultatGenerator.CreateSokeResultatMinimum(sok)
                    });
                    break;
                case ResponsType.Noekler:
                    ;
                    meldinger.Add(new Melding
                    {
                        FileName = "resultat.xml",
                        MeldingsType = FiksArkivMeldingtype.SokResultatNoekler,
                        ResultatMelding = _sokeresultatGenerator.CreateSokeResultatNoekler(),
                    });
                    break;
                case ResponsType.Utvidet:
                    meldinger.Add(new Melding
                    {
                        FileName = "resultat.xml",
                        MeldingsType = FiksArkivMeldingtype.SokResultatUtvidet,
                        ResultatMelding = _sokeresultatGenerator.CreateSokeResultatUtvidet(sok, lagretArkivmeldinger)
                    });
                    break;
                default:
                    meldinger.Add(new Melding
                    {
                        FileName = "resultat.xml",
                        MeldingsType = FiksArkivMeldingtype.SokResultatMinimum,
                        ResultatMelding = _sokeresultatGenerator.CreateSokeResultatMinimum(sok),
                    });
                    break;
            }

            return meldinger;
        }
    }
}