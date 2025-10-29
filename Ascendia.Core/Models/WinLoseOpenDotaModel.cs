using System.Text.Json.Serialization;

namespace Ascendia.Core.Models
{
    public class WinLoseOpenDotaModel
    {
        [JsonPropertyName("lose")]
        public int Lose { get; set; }

        [JsonPropertyName("win")]
        public int Win { get; set; }
    }
}