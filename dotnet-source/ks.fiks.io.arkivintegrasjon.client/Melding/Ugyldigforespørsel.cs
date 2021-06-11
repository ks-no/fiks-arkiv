using System.Runtime.CompilerServices;
using System.Text.Json;

namespace ks.fiks.io.arkivintegrasjon.client.ForenkletArkivering
{
    public class Ugyldigforespørsel
    {
        public string errorId { get; set; }
        public string feilmelding { get; set; }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}