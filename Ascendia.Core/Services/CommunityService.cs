using Ascendia.Core.Interactivity;
using Ascendia.Core.Models;
using Ascendia.Core.Records;
using Ascendia.Core.Strings;
using LCTWorks.Core.Helpers;
using LCTWorks.Core.Services;
using LCTWorks.Telemetry;
using Microsoft.Extensions.Logging;
using System.Diagnostics.Metrics;
using System.Numerics;
using System.Xml.Linq;

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
    private const string SettingsCacheFileName = "settings.json";
    private readonly AirtableHttpService _airtableService = airtableService;
    private readonly CacheService _cacheService = cacheService;
    private readonly List<GuildSettingsModel> _guildSettings = [];
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
        string? notes = null,
        bool checkLadder = true,
        bool refreshPlayer = true,
        bool checkWinLose = true,
        string? socialFacebook = null,
        string? socialInstagram = null,
        string? socialX = null,
        string? socialTikTok = null,
        string? socialYouTube = null,
        string? socialTwitch = null,
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
        return await UpdateLadderAsync(accountId, accountId, name, team, phone, email, country, isCaptain, position, notes, checkLadder, refreshPlayer, checkWinLose, socialFacebook, socialInstagram, socialX, socialTikTok, socialYouTube, socialTwitch, notifications, null, messageDelayInMilliseconds);
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
        string? notes = null,
        string? socialFacebook = null,
        string? socialInstagram = null,
        string? socialX = null,
        string? socialTikTok = null,
        string? socialYouTube = null,
        string? socialTwitch = null,
        EventHandler<string>? notifications = null)
    {
        if (IsBusy)
        {
            return false;
        }
        IsBusy = true;

        notifications?.Invoke(this, Messages.RefreshingMembers);

        var members = await GetAllMembersAsync(true);
        var containedMember = members.FirstOrDefault(x => x.Id == id);

        notifications?.Invoke(this, Messages.ProgressAddToDb);

        MemberRecord record = CreateMemberRecord(id, name, accountId, team, phone, email, country, isCaptain, position, notes, socialFacebook, socialInstagram, socialX, socialTikTok, socialYouTube, socialTwitch, previousRecord: containedMember);
        IsBusy = false;
        return await AddOrUpdateMemberAsync(record);
    }

    public async Task<List<GuildSettingsModel>?> GetAllGuildSettingsAsync(bool forceRefresh = false)
    {
        if (forceRefresh || _guildSettings.Count == 0)
        {
            var guildSettings = await _airtableService.GetDiscordBotGuildsSettingsAsync();
            if (guildSettings != null && guildSettings.Any())
            {
                _guildSettings.Clear();
                _guildSettings.AddRange(guildSettings.Select(item => new GuildSettingsModel(item)));
                await SaveToCacheAsync(false, true);
            }
        }
        return _guildSettings;
    }

    public async Task<List<MemberRecord>> GetAllMembersAsync(bool forceRefresh = false)
    {
        if (IsBusy)
        {
            return _members;
        }
        IsBusy = true;
        if (_members.Count == 0 || forceRefresh)
        {
            var records = await _airtableService.GetMemberRecordsAsync();
            if (records != null)
            {
                _members.Clear();
                _members.AddRange(records.OrderBy(x => x.DisplayName));
                await SaveToCacheAsync();
            }
        }

        IsBusy = false;
        return _members;
    }

    public GuildSettingsModel? GetGuildSettings(ulong guildId)
    {
        if (_guildSettings == null || _guildSettings.Count == 0)
        {
            return default;
        }
        return _guildSettings.FirstOrDefault(x => x.GuildId == guildId);
    }

    public async Task InitializeFromCacheAsync()
    {
        var cachedMembers = await _cacheService.GetCachedTextAsync(MembersCacheFileName);
        if (!string.IsNullOrEmpty(cachedMembers))
        {
            var ordered = (Json.ToObject<List<MemberRecord>>(cachedMembers) ?? []).OrderBy(x => x.DisplayName);
            _members.AddRange(ordered);
        }
        var settings = await _cacheService.GetCachedTextAsync(SettingsCacheFileName);
        if (!string.IsNullOrEmpty(settings))
        {
            var guildSettings = Json.ToObject<List<GuildSettingsModel>>(settings) ?? [];
            _guildSettings.AddRange(guildSettings);
        }
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
            await SaveToCacheAsync();
        }
        IsBusy = false;
        return results;
    }

    public async Task<bool> UpdateLadderAsync(
        string id,
        string accountId,
        string? name = null,
        string? team = null,
        string? phone = null,
        string? email = null,
        string? country = null,
        bool? isCaptain = false,
        string? position = null,
        string? notes = null,
        bool checkLadder = true,
        bool refreshPlayer = true,
        bool checkWinLose = true,
        string? socialFacebook = null,
        string? socialInstagram = null,
        string? socialX = null,
        string? socialTikTok = null,
        string? socialYouTube = null,
        string? socialTwitch = null,
        EventHandler<string>? notifications = null,
        MemberRecord? previousRecord = null,
        int messageDelayInMilliseconds = MessageDelayInMilliseconds)
    {
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
            var playerResult = await _ladderService.GetPlayerAsync(accountId);

            await Task.Delay(messageDelayInMilliseconds);
            if (!playerResult.Valid)
            {
                notifications?.Invoke(this, Messages.ProgressPlayerNotFound);
                await Task.Delay(messageDelayInMilliseconds);
            }
            else
            {
                player = playerResult.Value;
                if (checkWinLose)
                {
                    notifications?.Invoke(this, Messages.ProgressCheckWinLose);
                    var winLoseResult = await _ladderService.GetPlayerWinLoseAsync(accountId);
                    await Task.Delay(messageDelayInMilliseconds);
                    if (!winLoseResult.Valid)
                    {
                        notifications?.Invoke(this, Messages.ProgressWinLoseNotFound);
                        await Task.Delay(messageDelayInMilliseconds);
                    }
                    winLose = winLoseResult.Value;
                }
            }
        }

        notifications?.Invoke(this, Messages.ProgressAddToDb);
        if (string.IsNullOrWhiteSpace(name))
        {
            name = previousRecord?.DisplayName ?? player?.Profile?.PersonaName ?? Messages.DefaultPlayerName;
        }

        MemberRecord record = CreateMemberRecord(id, name, accountId, team, phone, email, country, isCaptain, position, notes, socialFacebook, socialInstagram, socialX, socialTikTok, socialYouTube, socialTwitch,
            player: player,
            winLose: winLose,
            previousRecord);

        var addResult = await AddOrUpdateMemberAsync(record);
        IsBusy = false;
        return addResult;
    }

    public async Task<int> UpdatePlayersAsync(bool incudeWL = true, EventHandler<string>? notifications = null, int minutesUpdateThreshold = 0, CancellationToken? cancellationToken = null)
    {
        CoreTelemetry.WriteLine(Messages.RefreshingMembers);
        //Refresh so we have the latest data, including last updated timestamps.
        var members = await GetAllMembersAsync(true);

        IsBusy = true;
        var membersToUpdate = members
            .Where(m => m.IsEnabled)
            .Where(m => minutesUpdateThreshold <= 0 || m.LastUpdated?.AddMinutes(minutesUpdateThreshold) <= DateTimeOffset.UtcNow.DateTime)
            //.Where(m => m.AccountId == "190234148")
            .OrderBy(m => m.LastUpdated)
            .Take(2)
            .ToList();

        if (minutesUpdateThreshold > 0)
        {
            int skipped = members.Count - membersToUpdate.Count;
            if (skipped > 0)
            {
                CoreTelemetry.WriteLine(Messages.UpToDateMembersSkippedFormat);
            }
        }

        var count = membersToUpdate.Count;
        int index = 0;
        var updatedRecords = new List<MemberRecord>();
        foreach (var member in membersToUpdate)
        {
            if (cancellationToken?.IsCancellationRequested == true)
            {
                break;
            }
            var displayName = $"{member.DisplayName ?? "?"}";

            var progressMessage = string.Format(Messages.UpdateMemberProgressFormat, ++index, count, displayName);
            notifications?.Invoke(this, progressMessage);
            CoreTelemetry.WriteLine(progressMessage);

            if (member.DisplayName == null || member.AccountId == null)
            {
                continue;
            }
            WinLoseOpenDotaModel? winLoseData = null;
            PlayerOpenDotaModel? playerData = null;
            var retry = true;
            var refreshed = false;
            while (retry)
            {
                retry = !refreshed;

                if (!refreshed)
                {
                    var refreshResults = await _ladderService.RefreshPlayerAsync(member.AccountId);
                    retry = refreshResults.LimitReached;
                    refreshed = refreshResults.Valid;
                }
                if (!retry)
                {
                    if (playerData == null)
                    {
                        var playerResult = await _ladderService.GetPlayerAsync(member.AccountId);
                        retry = playerResult.LimitReached;
                        if (playerResult.Valid && !retry)
                        {
                            playerData = playerResult.Value;
                        }
                    }

                    if (playerData != null && incudeWL)
                    {
                        var winLoseResult = await _ladderService.GetPlayerWinLoseAsync(member.AccountId);
                        retry = winLoseResult.LimitReached;
                        if (winLoseResult.Valid && !retry)
                        {
                            winLoseData = winLoseResult.Value;
                        }
                    }
                }

                if (retry)
                {
                    //previous attempt failed. Notify the user and wait before retrying
                    var waitMessage = string.Format(Messages.ProgressRequestLimitReachedFormat, index, count);
                    notifications?.Invoke(this, waitMessage);
                    CoreTelemetry.WriteLine(waitMessage);

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
                            CoreTelemetry.WriteErrorLine(string.Format(Messages.ExceptionMessageFormat, e.Message));
                        }
                    }
                    _telemetryService.LogInformation(GetType(), message: Messages.ProgressWaitingFinished);
                }
                else
                {
                    if (!refreshed)
                    {
                        CoreTelemetry.WriteErrorLine(string.Format(Messages.ErrorResponseFormat, "/Refresh"));
                    }
                    else
                    {
                        if (playerData == null)
                        {
                            CoreTelemetry.WriteErrorLine(string.Format(Messages.ErrorResponseFormat, "/Player"));
                        }
                        if (incudeWL && winLoseData == null)
                        {
                            CoreTelemetry.WriteErrorLine(string.Format(Messages.ErrorResponseFormat, "/WL"));
                        }
                    }
                }

                if (!retry)
                {
                    var record = CreateMemberRecord(member.Id, member.DisplayName, member.AccountId, member.Team, member.Phone, member.Email, member.Country, member.IsCaptain, member.Position, member.Notes, player: playerData, winLose: winLoseData, previousRecord: member);
                    updatedRecords.Add(record);
                }
            }
        }
        await _airtableService.UpdateMultipleMembersAsync([.. updatedRecords]);
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
        string? notes = null,
        string? socialFacebook = null,
        string? socialInstagram = null,
        string? socialX = null,
        string? socialTikTok = null,
        string? socialYouTube = null,
        string? socialTwitch = null,
        PlayerOpenDotaModel? player = null,
        WinLoseOpenDotaModel? winLose = null,
        MemberRecord? previousRecord = null)
    {
        int previousLeaderboardRank = previousRecord?.PreviousLeaderboardRank ?? 0;
        int previousRankTier = previousRecord?.PreviousRankTier ?? 0;
        int leaderboardRank = player?.LeaderboardRank ?? previousRecord?.LeaderboardRank ?? 0;
        int rankTier = player?.RankTier ?? previousRecord?.RankTier ?? 0;
        DateTime? lastUpdated = previousRecord?.LastUpdated ?? DateTimeOffset.UtcNow.DateTime;
        if (player != null
            && previousRecord != null)
        {
            previousLeaderboardRank = leaderboardRank;
            previousRankTier = rankTier;
        }
        if (player != null)
        {
            lastUpdated = DateTimeOffset.UtcNow.DateTime;
        }

        var avatarUrl = player?.Profile?.Avatar ?? previousRecord?.AvatarUrl ?? string.Empty;
        var profileUrl = player?.Profile?.ProfileUrl ?? previousRecord?.ProfileUrl ?? string.Empty;
        var win = winLose?.Win ?? previousRecord?.Win ?? 0;
        var lose = winLose?.Lose ?? previousRecord?.Lose ?? 0;
        var mmr = player?.ComputerMMR ?? previousRecord?.MMR ?? 0d;
        var isEnabled = previousRecord?.IsEnabled ?? true;
        if (string.IsNullOrEmpty(position))
        {
            position = previousRecord?.Position ?? 0.ToString();
        }

        return new MemberRecord(id)
        {
            AccountId = accountId,
            DisplayName = name,
            AccountName = player?.Profile?.PersonaName ?? name,
            AvatarUrl = avatarUrl,
            ProfileUrl = profileUrl,
            LeaderboardRank = leaderboardRank,
            RankTier = rankTier,
            PreviousLeaderboardRank = previousLeaderboardRank,
            PreviousRankTier = previousRankTier,
            Phone = phone ?? string.Empty,
            Email = email ?? string.Empty,
            Country = country ?? string.Empty,
            IsCaptain = isCaptain,
            Position = position ?? 0.ToString(),
            Win = win,
            Lose = lose,
            MMR = mmr,
            LastUpdated = lastUpdated,
            LastChange = DateTimeOffset.UtcNow.DateTime,
            IsEnabled = isEnabled,
            Team = team ?? string.Empty,
            Notes = notes ?? string.Empty,
            SocialFacebook = socialFacebook ?? string.Empty,
            SocialInstagram = socialInstagram ?? string.Empty,
            SocialX = socialX ?? string.Empty,
            SocialTikTok = socialTikTok ?? string.Empty,
            SocialYouTube = socialYouTube ?? string.Empty,
            SocialTwitch = socialTwitch ?? string.Empty
        };
    }

    private Task<bool> AddOrUpdateMemberAsync(MemberRecord? record)
        => _airtableService.CreateOrEditMemberAsync(record);

    private async Task SaveToCacheAsync(bool saveMembers = true, bool saveSettings = false)
    {
        try
        {
            if (saveMembers && _members != null && _members.Count > 0)
            {
                var json = Json.Stringify(_members);
                await _cacheService.CacheTextFileAsync(MembersCacheFileName, json);
            }
            if (saveSettings && _guildSettings != null && _guildSettings.Count > 0)
            {
                var json = await Json.StringifyAsync(_guildSettings);
                await _cacheService.CacheTextFileAsync(SettingsCacheFileName, json);
            }
        }
        catch
        {
        }
    }
}