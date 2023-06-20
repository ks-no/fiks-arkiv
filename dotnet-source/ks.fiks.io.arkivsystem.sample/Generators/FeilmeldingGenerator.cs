using System;
using System.Collections.Generic;
using System.Linq;
using KS.Fiks.Arkiv.Models.V1.Feilmelding;

namespace ks.fiks.io.arkivsystem.sample.Generators
{
    public class FeilmeldingGenerator
    {
        public static Ugyldigforespoersel CreateUgyldigforespoerselMelding(IReadOnlyList<List<string>> validationResult)
        {
            string feilmelding = null;
            foreach (var meldinger in validationResult)
            {
                feilmelding = meldinger.Aggregate(feilmelding, (current, melding) => current + $"{Environment.NewLine} Feilmelding {melding}");
            }
            return new Ugyldigforespoersel()
            {
                FeilId = Guid.NewGuid().ToString(),
                Feilmelding = $"Feilmelding: {feilmelding}",
            };
        }

        public static Ugyldigforespoersel CreateUgyldigforespoerselMelding(string feilmelding)
        {
            return new Ugyldigforespoersel()
            {
                FeilId = Guid.NewGuid().ToString(),
                Feilmelding = feilmelding,
            };
        }
        
        public static Ikkefunnet CreateIkkefunnetMelding(string feilmelding)
        {
            return new Ikkefunnet()
            {
                FeilId = Guid.NewGuid().ToString(),
                Feilmelding = feilmelding,
            };
        }

        
        public static Serverfeil CreateServerFeilMelding(string feilmelding)
        {
            return new Serverfeil()
            {
                FeilId = Guid.NewGuid().ToString(),
                Feilmelding = feilmelding,
            };
        }
    }
}