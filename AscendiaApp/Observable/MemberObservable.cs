using Ascendia.Core.Records;
using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace AscendiaApp.Observable;

public partial class MemberObservable(MemberRecord record) : ObservableObject
{
    private readonly MemberRecord _record = record;

    public string AccountName => _record.AccountName ?? string.Empty;

    public string AvatarUrl => _record.AvatarUrl ?? "ms-appx:///Assets/App/DefaultProfile.png";

    public string Caption
    {
        get
        {
            return string.Join(" • ", _record.AccountName, _record.RankTier, _record.LeaderboardRank);
        }
    }

    public string DisplayName => _record.DisplayName ?? string.Empty;

    public bool IsCaptain => _record.IsCaptain ?? false;

    public bool IsOutDated
        => !IsUpToDate;

    public bool IsUpToDate
        => _record.LastUpdated.HasValue && (DateTimeOffset.UtcNow - _record.LastUpdated.Value).TotalHours < 1;

    public string Leaderboard => $"{(_record.LeaderboardRank ?? 0):0000}";

    public string? Position => _record.Position;

    public int? RankTier => _record.RankTier;

    public MemberRecord Record => _record;

    public string Team => _record.Team ?? string.Empty;

    public string GetWinRate()
    {
        var total = _record.Win + _record.Lose ?? 0;
        if (total == 0)
        {
            return $"{total} | --";
        }
        double winRate = (_record.Win ?? 0) / (double)(total) * 100;
        return $"{total} | {winRate:F0}%";
    }

    public override string ToString()
                => DisplayName;
}