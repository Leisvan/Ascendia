using Ascendia.Core.Records;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AscendiaApp.Observable;

public partial class MemberObservable(MemberRecord record) : ObservableObject
{
    private readonly MemberRecord _record = record;

    public string AvatarUrl => _record.AvatarUrl ?? "ms-appx:///Assets/App/DefaultProfile.png";

    public string Caption
    {
        get
        {
            return string.Join(" • ", _record.AccountName, _record.RankTier, _record.LeaderboardRank);
        }
    }

    public string DisplayName => _record.DisplayName ?? string.Empty;

    public MemberRecord Record => _record;

    public string Team => _record.Team ?? string.Empty;

    public override string ToString()
        => DisplayName;
}