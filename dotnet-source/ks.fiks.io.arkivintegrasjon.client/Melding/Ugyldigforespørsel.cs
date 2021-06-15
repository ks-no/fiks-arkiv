using System;
using System.Text.Json.Serialization;

namespace ks.fiks.io.arkivintegrasjon.client.Melding
{
    public class Ugyldigforespørsel
    {
        [JsonPropertyName("errorId")]
        public string ErrorId { get; set; }
        
        [JsonPropertyName("feilmelding")]
        public string Feilmelding { get; set; }
        
        [JsonPropertyName("referanseMeldingId")]
        public Guid ReferanseMeldingId { get; set; }
    }
}