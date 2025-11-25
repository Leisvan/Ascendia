using Ascendia.Core;
using Ascendia.Core.Records;
using Ascendia.Core.Services;
using Ascendia.Discord.Strings;
using CommunityToolkit.HighPerformance;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using System.Globalization;
using System.Text;

namespace Ascendia.Discord.Internal;

internal class GuildActionsService(
        CommunityService communityDataService,
        DiscordBotService botService,
        LadderService ladderService,
        InteractivityExtension interactivity)
{
    private const string BlankSpace = " ";
    private const string DoubleSpaceCode = "`  `";
    private const int RankingLeadersChunkCount = 2;
    private const int RankingMessageChunkSize = 8;
    private static readonly CultureInfo CultureInfo = new("es-ES");
    private readonly DiscordBotService _botService = botService;
    private readonly CommunityService _communityDataService = communityDataService;
    private readonly InteractivityExtension _interactivity = interactivity;
    private readonly LadderService _ladderService = ladderService;
    private CancellationTokenSource? _cts;

    public void CancelOperation()
    {
        if (_cts != null && !_cts.IsCancellationRequested)
        {
            CoreTelemetry.WriteLine(Messages.OperationCancelling);
            _cts.Cancel();
        }
    }

    public async Task<string?> DisplayRankingAsync(
            bool includeBanned = false,
            ulong channelId = 0,
            CommandContext? context = null)
    {
        if (_cts != null)
        {
            return Messages.OperationAlredyInProgress;
        }
        _cts = new CancellationTokenSource();

        var rankComparer = new MemberRecordRankingComparer();

        CoreTelemetry.WriteLine(Messages.RefreshingMembers);

        var membersList = await _communityDataService.GetAllMembersAsync(true);
        var filteredMembers = membersList
            .Where(x => includeBanned || x.IsEnabled)
            .Where(x => x.LeaderboardRank > 0 || (x.RankTier != 80 && x.RankTier > 0))
            .Order(rankComparer);
        var lines = new List<string>();
        foreach (var member in filteredMembers)
        {
            lines.Add(await GetMemberLineAsync(member));
        }
        var channel = context == null
               ? await _botService.Client.GetChannelAsync(channelId)
               : context.Channel;

        if (lines.Count == 0)
        {
            await SendMessageAsync(channel, Messages.NoMembersToShow);
            return Messages.NoMembersToShow;
        }

        await SendMessageAsync(channel, Messages.RankingHeader);

        var headerString = GetRankingHeaderString();
        await SendMessageAsync(channel, $"`##` {headerString}");

        var chunks = lines.Chunk(RankingMessageChunkSize);
        var leadersChunks = chunks.Take(RankingLeadersChunkCount);
        var seq = 1;

        foreach (var chunk in leadersChunks)
        {
            if (_cts?.Token.IsCancellationRequested == true)
            {
                await SendMessageAsync(channel, Messages.OperationCancelled);
                _cts = null;
                return Messages.OperationCancelled;
            }
            var stringBuilder = new StringBuilder();
            foreach (var line in chunk)
            {
                var seqLine = $"`{seq++:00}` {line}";
                stringBuilder.AppendLine(seqLine);
            }
            var allLines = stringBuilder.ToString();
            await SendMessageAsync(channel, allLines);
        }

        if (lines.Count > RankingMessageChunkSize * RankingLeadersChunkCount)
        {
            await SendMessageAsync(channel, Messages.FullRankingCaption);
            var threadName = string.Format(Messages.FullRankingThreadNameFormat, lines.Count);
            var threadChannel = await CreateThreadAsync(threadName, channel);
            if (!threadChannel.IsThread)
            {
                await SendMessageAsync(channel, Messages.ThreadCreationFailed);
                return Messages.ThreadCreationFailed;
            }
            await SendMessageAsync(threadChannel, $"`###` {headerString}");
            int seqx = 1;
            foreach (var chunk in chunks)
            {
                if (_cts?.Token.IsCancellationRequested == true)
                {
                    await SendMessageAsync(threadChannel, Messages.OperationCancelled);
                    await SendMessageAsync(channel, Messages.OperationCancelled);
                    _cts = null;
                    return Messages.OperationCancelled;
                }
                var stringBuilder = new StringBuilder();

                foreach (var line in chunk)
                {
                    var seqLine = $"`{seqx++:000}` {line}";
                    stringBuilder.AppendLine(seqLine);
                }
                var allLines = stringBuilder.ToString();
                await SendMessageAsync(threadChannel, allLines);
            }
        }
        return null;
    }

    public async Task<string?> UpdateMembersLadderAsync(
                    bool forceUpdate = false,
                    bool includeWL = true,
                    bool notify = true,
                    ulong guildId = 0,
                    ulong channelId = 0,
                    CommandContext? context = null)
    {
        if (_cts != null)
        {
            return Messages.OperationAlredyInProgress;
        }
        _cts = new CancellationTokenSource();

        int minutesUpdateThreshold = 0;
        if (!forceUpdate)
        {
            var guildSettings = _communityDataService.GetGuildSettings(guildId);
            minutesUpdateThreshold = guildSettings?.RegionUpdateThresholdInMinutes ?? 0;
        }

        DiscordMessage? message = null;
        if (context != null)
        {
            string messageContent = Messages.AccessingMembersList;
            var builder = new DiscordMessageBuilder()
                .WithContent(messageContent)
                .AddActionRowComponent(InteractionsHelper.GetCancelUpdateRankButton());
            CoreTelemetry.WriteLine(messageContent);
            message = await context.EditResponseAsync(builder);
        }

        var result = await _communityDataService.UpdateAllLaddersAsync(
            includeWL,
            async (s, e) =>
            {
                if (notify)
                {
                    message = await UpdateMessageAsync(e, channelId, message, false);
                }
                else
                {
                    CoreTelemetry.WriteLine(e);
                }
            },
            minutesUpdateThreshold: minutesUpdateThreshold,
            cancellationToken: _cts.Token);

        if (result == -1 || _cts?.Token.IsCancellationRequested == true)
        {
            if (notify)
            {
                await UpdateMessageAsync(Messages.OperationCancelled, channelId, message);
            }
            else
            {
                CoreTelemetry.WriteLine(Messages.OperationCancelled);
            }
            _cts = null;
            return Messages.OperationCancelled;
        }
        else
        {
            var updateMsg = string.Format(Messages.UpdatedProfilesCountFormat, result.ToString());
            if (notify)
            {
                await UpdateMessageAsync(updateMsg, channelId, message, true);
            }
            else
            {
                CoreTelemetry.WriteLine(updateMsg);
            }
        }
        _cts = null;
        return null;
    }

    private static async Task<DiscordChannel> CreateThreadAsync(string threadName, DiscordChannel channel)
    {
        try
        {
            if (channel.IsThread)
            {
                return channel;
            }
            else
            {
                return await channel.CreateThreadAsync(threadName, DiscordAutoArchiveDuration.Day, DiscordChannelType.PublicThread);
            }
        }
        catch (Exception ex)
        {
            CoreTelemetry.WriteWarningLine($"{Messages.ThreadCreationFailed}: {ex.Message}");
            return channel;
        }
    }

    private static string GetRankingHeaderString()
    {
        return
              DoubleSpaceCode + BlankSpace // League emoji
            + "`RANK`" + BlankSpace
            + DoubleSpaceCode + BlankSpace // Position emoji
            + $"`{StringLengthCapTool.Default.GetString("NICK")}`" + BlankSpace // Nick
            + "`  WR`" + BlankSpace
            + "`TOTAL`";
    }

    private static async Task SendMessageAsync(DiscordChannel channel, string content)
    {
        try
        {
            await channel.SendMessageAsync(content);
            CoreTelemetry.WriteLine(content, ConsoleColor.White);
        }
        catch (Exception e)
        {
            CoreTelemetry.WriteWarningLine(e.Message);
        }
    }

    private async Task<string> GetMemberLineAsync(MemberRecord member)
    {
        var builder = new StringBuilder();

        var rankEmoji = await EmojisHelper.GetRankEmojiStringAsync(_botService.Client, member.RankTier);

        string positionEmoji = string.Empty;
        if (int.TryParse(member.Position, out var positionValue))
        {
            positionEmoji = await EmojisHelper.GetPositionEmojiStringAsync(_botService.Client, positionValue);
        }

        var total = (member.Win ?? 0) + (member.Lose ?? 0);
        int? winratePercent = total == 0
            ? null
            : (int)Math.Round(((member.Win ?? 0) / (double)total) * 100);

        var winrateString = winratePercent == null
            ? "  --"
            : $"{StringLengthCapTool.InvertedThreeSpaces.GetString(winratePercent.Value)}%";

        int rankChange = 0;
        if (member.PreviousLeaderboardRank != member.LeaderboardRank || member.PreviousRankTier != member.RankTier)
        {
            rankChange = member.PreviousRankTier - member.RankTier ?? 0;
            if (rankChange == 0)
            {
                rankChange = member.PreviousLeaderboardRank - member.LeaderboardRank ?? 0;
            }
        }

        var changeGlyph = rankChange > 0
            ? '↑'
            : rankChange < 0
                ? '↓'
                : '-';

        builder.Append($"{rankEmoji} ");
        builder.Append($"`{changeGlyph}{StringLengthCapTool.InvertedFourSpaces.GetString(member.LeaderboardRank ?? 0)}` ");
        builder.Append($"|{positionEmoji} ");
        builder.Append($"`{StringLengthCapTool.Default.GetString(member.DisplayName)}` ");
        builder.Append($"`{winrateString}` ");
        builder.Append($"`{StringLengthCapTool.InvertedFiveSpaces.GetString(total)}` ");

        return builder.ToString();
    }

    private async Task<DiscordMessage?> UpdateMessageAsync(string content, ulong channelId, DiscordMessage? message = null, bool removeComponents = false)
    {
        try
        {
            if (message == null)
            {
                var channel = await _botService.Client.GetChannelAsync(channelId);
                var results = await channel.SendMessageAsync(content);
                CoreTelemetry.WriteLine(content, ConsoleColor.White);
                return results;
            }
            if (removeComponents)
            {
                var builder = new DiscordMessageBuilder()
                    .WithContent(content); // No components added => existing components cleared
                var results = await message.ModifyAsync(builder);
                CoreTelemetry.WriteLine(content, ConsoleColor.White);
                return results;
            }

            var mResults = await message.ModifyAsync(content);
            CoreTelemetry.WriteLine(content, ConsoleColor.White);
            return mResults;
        }
        catch (Exception e)
        {
            CoreTelemetry.WriteWarningLine(e.Message);
            return null;
        }
    }
}