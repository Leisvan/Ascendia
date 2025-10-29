using AirtableApiClient;
using Ascendia.Core.Records;

namespace Ascendia.Core.Extensions;

public static class AirtableExtensions
{
    public static AirtableRecord ToAirtableRecord(this MemberRecord record)
    {
        var newRecord = new AirtableRecord()
        {
            Id = record.Id,
            Fields = new Dictionary<string, object?>
            {
                { nameof(MemberRecord.DisplayName), record.DisplayName },
                { nameof(MemberRecord.AccountName), record.AccountName },
                { nameof(MemberRecord.AccountId), record.AccountId },
                { nameof(MemberRecord.IsEnabled), record.IsEnabled },
                { nameof(MemberRecord.Phone), record.Phone },
                { nameof(MemberRecord.Email), record.Email },
                { nameof(MemberRecord.Country), record.Country},
                { nameof(MemberRecord.IsCaptain), record.IsCaptain},
                { nameof(MemberRecord.Position), record.Position},
                { nameof(MemberRecord.AvatarUrl), record.AvatarUrl },
                { nameof(MemberRecord.ProfileUrl), record.ProfileUrl },
                { nameof(MemberRecord.LeaderboardRank), record.LeaderboardRank },
                { nameof(MemberRecord.RankTier), record.RankTier },
                { nameof(MemberRecord.PreviousLeaderboardRank), record.PreviousLeaderboardRank },
                { nameof(MemberRecord.PreviousRankTier), record.PreviousRankTier },
                { nameof(MemberRecord.Win), record.Win },
                { nameof(MemberRecord.Lose), record.Lose },
                { nameof(MemberRecord.Notes), record.Notes },
                { nameof(MemberRecord.LastUpdated), record.LastUpdated },
                { nameof(MemberRecord.LastChange), record.LastChange },
                { nameof(MemberRecord.Team), record.Team }
            }
        };
        var fieldsToList = newRecord.Fields.ToList();
        foreach (var item in fieldsToList)
        {
            if (item.Value == null)
            {
                newRecord.Fields.Remove(item.Key);
            }
        }
        return newRecord;
    }

    public static MemberRecord ToMemberRecord(this AirtableRecord record)
            => new(
            record.Id,
            Number: record.GetField<int>(nameof(MemberRecord.Number)),
            DisplayName: record.GetField<string?>(nameof(MemberRecord.DisplayName)),
            AccountName: record.GetField<string?>(nameof(MemberRecord.AccountName)),
            AccountId: record.GetField<string?>(nameof(MemberRecord.AccountId)),
            IsEnabled: record.GetField<bool>(nameof(MemberRecord.IsEnabled)),
            Phone: record.GetField<string?>(nameof(MemberRecord.Phone)),
            Email: record.GetField<string?>(nameof(MemberRecord.Email)),
            Country: record.GetField<string?>(nameof(MemberRecord.Country)),
            IsCaptain: record.GetField<bool?>(nameof(MemberRecord.IsCaptain)),
            Position: record.GetField<string?>(nameof(MemberRecord.Position)),
            AvatarUrl: record.GetField<string?>(nameof(MemberRecord.AvatarUrl)),
            ProfileUrl: record.GetField<string?>(nameof(MemberRecord.ProfileUrl)),
            LeaderboardRank: record.GetField<int?>(nameof(MemberRecord.LeaderboardRank)),
            RankTier: record.GetField<int?>(nameof(MemberRecord.RankTier)),
            PreviousLeaderboardRank: record.GetField<int?>(nameof(MemberRecord.PreviousLeaderboardRank)),
            PreviousRankTier: record.GetField<int?>(nameof(MemberRecord.PreviousRankTier)),
            Win: record.GetField<int?>(nameof(MemberRecord.Win)),
            Lose: record.GetField<int?>(nameof(MemberRecord.Lose)),
            Notes: record.GetField<string?>(nameof(MemberRecord.Notes)),
            LastUpdated: record.GetField<DateTime?>(nameof(MemberRecord.LastUpdated)),
            LastChange: record.GetField<DateTime?>(nameof(MemberRecord.LastChange)),
            Team: record.GetField<string?>(nameof(MemberRecord.Team)));
}