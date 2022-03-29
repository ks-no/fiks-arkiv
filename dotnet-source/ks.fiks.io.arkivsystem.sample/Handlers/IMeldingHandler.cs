using ks.fiks.io.arkivsystem.sample.Models;
using KS.Fiks.IO.Client.Models;

namespace ks.fiks.io.arkivsystem.sample.Handlers
{
    public interface IMeldingHandler
    {
        public Melding HandleMelding(MottattMeldingArgs mottattMeldingArgs);
    }
}