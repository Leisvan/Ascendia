using Ascendia.Core.Models;
using Ascendia.Core.Records;
using Ascendia.Core.Strings;
using LCTWorks.Core.Helpers;
using LCTWorks.Core.Services;
using LCTWorks.Telemetry;
using Microsoft.Extensions.Logging;

namespace Ascendia.Core.Services;

public class CommunityService(
    CacheService cacheService,
    AirtableHttpService airtableService,
    LadderService ladderService,
   ITelemetryService telemetryService)
{
    private const string MembersCacheFileName = "members.json";
    private const int MessageDelayInMilliseconds = 2000;
    private const int RequestLimitWaitTimeInMilliseconds = 65000;
    private const int RequestRetryLimit = 2;
    private readonly AirtableHttpService _airtableService = airtableService;
    private readonly CacheService _cacheService = cacheService;
    private readonly LadderService _ladderService = ladderService;
    private readonly List<MemberRecord> _members = [];
    private readonly ITelemetryService _telemetryService = telemetryService;

    public bool IsBusy { get; private set; } = false;

    public async Task<bool> AddNewMemberAsync(
        string? name,
        string accountId,
        string? team = null,
        string? phone = null,
        string? email = null,
        string? country = null,
        bool? isCaptain = false,
        string? position = null,
        bool checkLadder = true,
        bool refreshPlayer = true,
        bool checkWinLose = true,
        EventHandler<string>? notifications = null,
        int messageDelayInMilliseconds = MessageDelayInMilliseconds)
    {
        if (IsBusy)
        {
            return false;
        }
        if (MemberExists(accountId))
        {
            throw new DuplicateWaitObjectException(accountId, Messages.ErrorPlayerDuplicated);
        }
        IsBusy = true;

        PlayerOpenDotaModel? player = null;
        WinLoseOpenDotaModel? winLose = null;

        if (checkLadder)
        {
            if (refreshPlayer)
            {
                notifications?.Invoke(this, Messages.ProgressRefreshPlayer);
                await _ladderService.RefreshPlayerAsync(accountId);
                await Task.Delay(messageDelayInMilliseconds);
            }
            notifications?.Invoke(this, Messages.ProgressCheckLadder);
            player = await _ladderService.GetPlayerAsync(accountId);
            await Task.Delay(messageDelayInMilliseconds);
            if (player == null)
            {
                notifications?.Invoke(this, Messages.ProgressPlayerNotFound);
                await Task.Delay(messageDelayInMilliseconds);
            }
            else
            {
                if (checkWinLose)
                {
                    notifications?.Invoke(this, Messages.ProgressCheckWinLose);
                    winLose = await _ladderService.GetPlayerWinLoseAsync(accountId);
                    await Task.Delay(messageDelayInMilliseconds);
                    if (winLose == null)
                    {
                        notifications?.Invoke(this, Messages.ProgressWinLoseNotFound);
                        await Task.Delay(messageDelayInMilliseconds);
                    }
                }
            }
        }

        notifications?.Invoke(this, Messages.ProgressAddToDb);
        if (string.IsNullOrWhiteSpace(name))
        {
            name = player?.Profile?.PersonaName ?? Messages.DefaultPlayerName;
        }

        MemberRecord record = CreateMemberRecord("", name, accountId, team, phone, email, country, isCaptain, position, player, winLose);
        IsBusy = false;
        return await AddOrUpdateMemberAsync(record);
    }

    public async Task<bool> EditMemberAsync(
        string id,
        string name,
        string accountId,
        string? team = null,
        string? phone = null,
        string? email = null,
        string? country = null,
        bool? isCaptain = false,
        string? position = null,
        EventHandler<string>? notifications = null)
    {
        if (IsBusy)
        {
            return false;
        }
        IsBusy = true;

        notifications?.Invoke(this, Messages.ProgressAddToDb);

        MemberRecord record = CreateMemberRecord(id, name, accountId, team, phone, email, country, isCaptain, position);
        IsBusy = false;
        return await AddOrUpdateMemberAsync(record);
    }

    public async Task<List<MemberRecord>> GetAllMembersAsync(bool forceRefresh = false)
    {
        if (IsBusy)
        {
            return _members;
        }
        IsBusy = true;
        if (_members.Count == 0 && !forceRefresh)
        {
            await LoadMembersFromCacheAsync();
        }
        if (_members.Count == 0 || forceRefresh)
        {
            var records = await _airtableService.GetMemberRecordsAsync();
            if (records != null)
            {
                _members.Clear();
                _members.AddRange(records.OrderBy(x => x.DisplayName));
                await SaveMembersToCacheAsync();
            }
        }

        IsBusy = false;
        return _members;
    }

    public bool MemberExists(string accountId)
        => _members.Any(m => m.AccountId == accountId);

    public async Task<bool> RemoveMemberAsync(string id)
    {
        if (IsBusy)
        {
            return false;
        }
        IsBusy = true;
        var first = _members.FirstOrDefault(m => m.Id == id);
        var results = await _airtableService.RemoveMemberAsync(id);
        if (results && first != null)
        {
            _members.Remove(first);
            await SaveMembersToCacheAsync();
        }
        IsBusy = false;
        return results;
    }

    public async Task<int> UpdateLadderAsync(bool incudeWL = true, EventHandler<string>? notifications = null, int messageDelayInMilliseconds = MessageDelayInMilliseconds, CancellationToken? cancellationToken = null)
    {
        //If updates to those > last hour, POST refresh.
        IsBusy = true;
        var membersToUpdate = _members
            .Where(m => m.IsEnabled)
            //.Where(m => m.AccountId == "190234148")
            .OrderBy(m => m.LastUpdated)
            .ToList();

        var count = membersToUpdate.Count;
        int index = 0;
        var updatedRecords = new List<MemberRecord>();
        foreach (var member in membersToUpdate)
        {
            if (cancellationToken?.IsCancellationRequested == true)
            {
                break;
            }
            var displayName = $"{member.DisplayName ?? "?"} ({member.AccountId ?? "?"})";

            notifications?.Invoke(this, string.Format(Messages.UpdateMemberProgressFormat, ++index, count, displayName));
            await Task.Delay(messageDelayInMilliseconds);
            _telemetryService.LogInformation(GetType(), message: $"Updating user {index} of {count}: {member.DisplayName} ({member.AccountId})");

            if (member.DisplayName == null || member.AccountId == null)
            {
                continue;
            }
            WinLoseOpenDotaModel? winLoseData = null;
            PlayerOpenDotaModel? playerData = null;
            int attempts = 1;
            while ((playerData == null || winLoseData == null) && attempts <= RequestRetryLimit)
            {
                if (attempts > 1)
                {
                    //previous attempt failed. Notify the user and wait before retrying
                    notifications?.Invoke(this, string.Format(Messages.ProgressRequestLimitReached, index, count));                   
                    _telemetryService.LogInformation(GetType(), message: $"Request limit reached. Waiting {RequestLimitWaitTimeInMilliseconds}ms.");
                    if (cancellationToken == null)
                    {
                        await Task.Delay(RequestLimitWaitTimeInMilliseconds);
                    }
                    else
                    {
                        try
                        {
                            await Task.Delay(RequestLimitWaitTimeInMilliseconds, cancellationToken.Value);
                        }
                        catch (Exception e)
                        {
                            if (cancellationToken?.IsCancellationRequested == true)
                            {
                                notifications?.Invoke(this, Messages.ProgressCancelling);
                                break;
                            }
                            _telemetryService.LogInformation(GetType(), message: $"Error: {e.Message}");
                        }
                    }
                    _telemetryService.LogInformation(GetType(), message: "Waiting finished.");
                }
                var refresh = await _ladderService.RefreshPlayerAsync(member.AccountId);
                if (refresh)
                {
                    playerData = await _ladderService.GetPlayerAsync(member.AccountId);
                    if (playerData != null && incudeWL)
                    {
                        winLoseData = await _ladderService.GetPlayerWinLoseAsync(member.AccountId);
                    }
                }
                attempts++;
            }

            var record = CreateMemberRecord(member.Id, member.DisplayName, member.AccountId, member.Team, member.Phone, member.Email, member.Country, member.IsCaptain, member.Position, playerData, winLoseData, member);
            updatedRecords.Add(record);
        }

        await _airtableService.UpdateMultipleMemberAsync([.. updatedRecords]);
        IsBusy = false;
        return updatedRecords.Count;
    }

    private static MemberRecord CreateMemberRecord(
        string id,
        string name,
        string accountId,
        string? team,
        string? phone,
        string? email,
        string? country = null,
        bool? isCaptain = null,
        string? position = null,
        PlayerOpenDotaModel? player = null,
        WinLoseOpenDotaModel? winLose = null,
        MemberRecord? previousRecord = null)
    {
        int previousLeaderboardRank = previousRecord?.PreviousLeaderboardRank ?? 0;
        int previousRankTier = previousRecord?.PreviousRankTier ?? 0;
        int leaderboardRank = player?.LeaderboardRank ?? 0;
        int rankTier = player?.RankTier ?? 0;
        DateTime? lastChange = previousRecord?.LastChange ?? DateTimeOffset.Now.DateTime;
        if (player != null
            && previousRecord != null
            && (leaderboardRank != previousRecord.LeaderboardRank || rankTier != previousRecord.RankTier))
        {
            previousLeaderboardRank = leaderboardRank;
            previousRankTier = rankTier;
            lastChange = DateTimeOffset.Now.DateTime;
        }

        return new MemberRecord(id)
        {
            AccountId = accountId,
            DisplayName = name,
            AccountName = player?.Profile?.PersonaName ?? name,
            AvatarUrl = player?.Profile?.Avatar ?? string.Empty,
            ProfileUrl = player?.Profile?.ProfileUrl ?? string.Empty,
            LeaderboardRank = leaderboardRank,
            RankTier = rankTier,
            PreviousLeaderboardRank = previousLeaderboardRank,
            PreviousRankTier = previousRankTier,
            Phone = phone ?? string.Empty,
            Email = email ?? string.Empty,
            Country = country ?? string.Empty,
            IsCaptain = isCaptain,
            Position = position ?? 0.ToString(),
            Win = winLose?.Win ?? 0,
            Lose = winLose?.Lose ?? 0,
            LastUpdated = DateTimeOffset.UtcNow.DateTime,
            LastChange = lastChange,
            IsEnabled = true,
            Team = team ?? string.Empty,
        };
    }

    private Task<bool> AddOrUpdateMemberAsync(MemberRecord? record)
        => _airtableService.CreateOrEditMemberAsync(record);

    private async Task LoadMembersFromCacheAsync()
    {
        var cachedMembers = await _cacheService.GetCachedTextAsync(MembersCacheFileName);
        if (!string.IsNullOrEmpty(cachedMembers))
        {
            var ordered = (Json.ToObject<List<MemberRecord>>(cachedMembers) ?? []).OrderBy(x => x.DisplayName);
            _members.AddRange(ordered);
        }
    }

    private async Task SaveMembersToCacheAsync()
    {
        var json = Json.Stringify(_members);
        await _cacheService.CacheTextFileAsync(MembersCacheFileName, json);
    }
}