using System;
using System.Collections.Generic;
using KS.Fiks.IO.Client.Models.Feilmelding;

namespace ks.fiks.io.arkivsystem.sample.Generators
{
    public class FeilmeldingGenerator
    {
        public static Ugyldigforespoersel CreateUgyldigforespoerselMelding(IReadOnlyList<List<string>> validationResult)
        {
            return new Ugyldigforespoersel()
            {
                ErrorId = Guid.NewGuid().ToString(),
                Feilmelding = $"Feilmelding: {Environment.NewLine} {validationResult[0]}",
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