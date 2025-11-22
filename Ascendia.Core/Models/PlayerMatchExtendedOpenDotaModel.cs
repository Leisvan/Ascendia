using System.Text.Json.Serialization;

namespace Ascendia.Core.Models;

public class PlayerMatchExtendedOpenDotaModel
{
    [JsonPropertyName("patch")]
    public int Patch { get; set; }

    [JsonPropertyName("region")]
    public int Region { get; set; }
}