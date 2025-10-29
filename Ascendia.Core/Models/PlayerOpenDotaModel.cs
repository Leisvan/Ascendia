using System.Text.Json.Serialization;

namespace Ascendia.Core.Models;

public class PlayerOpenDotaModel
{
    [JsonPropertyName("competitive_rank")]
    public int? CompetitiveRank { get; set; }

    [JsonPropertyName("leaderboard_rank")]
    public int? LeaderboardRank { get; set; }

    [JsonPropertyName("profile")]
    public ProfileOpenDotaModel? Profile { get; set; } = null;

    [JsonPropertyName("rank_tier")]
    public int? RankTier { get; set; }

    [JsonPropertyName("solo_competitive_rank")]
    public int? SoloCompetitiveRank { get; set; }
}