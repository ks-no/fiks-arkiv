using System;
using System.Collections.Generic;
using System.Linq;
using KS.Fiks.IO.Client.Models.Feilmelding;

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
                ErrorId = Guid.NewGuid().ToString(),
                Feilmelding = $"Feilmelding: {feilmelding}",
                CorrelationId = Guid.NewGuid().ToString()
            };
        }

        public static Ugyldigforespoersel CreateUgyldigforespoerselMelding(string feilmelding)
        {
            return new Ugyldigforespoersel()
            {
                ErrorId = Guid.NewGuid().ToString(),
                Feilmelding = feilmelding,
                CorrelationId = Guid.NewGuid().ToString()
            };
        }
    }
}