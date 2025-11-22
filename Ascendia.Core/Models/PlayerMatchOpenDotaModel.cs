using System.Text.Json.Serialization;

namespace Ascendia.Core.Models;

public class PlayerMatchOpenDotaModel
{
    [JsonPropertyName("assists")]
    public int Assists { get; set; }

    [JsonPropertyName("average_rank")]
    public int AverageRank { get; set; }

    [JsonPropertyName("deaths")]
    public int Deaths { get; set; }

    [JsonPropertyName("duration")]
    public int Duration { get; set; }

    [JsonPropertyName("game_mode")]
    public int GameMode { get; set; }

    [JsonPropertyName("hero_id")]
    public int HeroId { get; set; }

    [JsonPropertyName("hero_variant")]
    public int HeroVariant { get; set; }

    [JsonPropertyName("kills")]
    public int Kills { get; set; }

    [JsonPropertyName("leaver_status")]
    public int LeaverStatus { get; set; }

    [JsonPropertyName("lobby_type")]
    public int LobbyType { get; set; }

    [JsonPropertyName("match_id")]
    public long MatchId { get; set; }

    [JsonPropertyName("party_size")]
    public int? PartySize { get; set; }

    [JsonPropertyName("player_slot")]
    public int PlayerSlot { get; set; }

    [JsonPropertyName("radiant_win")]
    public bool RadiantWin { get; set; }

    [JsonPropertyName("start_time")]
    public long StartTime { get; set; }

    [JsonPropertyName("version")]
    public int? Version { get; set; }
}