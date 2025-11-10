using Ascendia.Core.Interactivity;
using Ascendia.Core.Services;
using Ascendia.Discord.Strings;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using System.Globalization;

namespace Ascendia.Discord.Internal;

internal class GuildActionsService(
        CommunityService communityDataService,
        DiscordBotService botService,
        LadderService ladderService,
        InteractivityExtension interactivity)
{
    private const string BlankSpace = " ";
    private const string DoubleSpaceCode = "`  `";
    private const int RankingMessageChunkSize = 8;
    private static readonly CultureInfo CultureInfo = new("es-ES");
    private readonly DiscordBotService _botService = botService;
    private readonly CommunityService _communityDataService = communityDataService;
    private readonly InteractivityExtension _interactivity = interactivity;
    private readonly LadderService _ladderService = ladderService;
    private CancellationTokenSource? _cts;

    public void CancelOperation()
    {
        _cts?.Cancel();
    }

    public async Task<string?> DisplayRankingAsync(
            bool includeBanned = false,
            ulong channelId = 0,
            CommandContext? context = null)
    {
        var members = await _communityDataService.GetAllMembersAsync();
        foreach (var member in members.Where(x => includeBanned || !x.IsEnabled))
        {
        }
        return null;
    }

    public async Task<string?> UpdateMemberRegionsAsync(
                    bool forceUpdate = false,
                    bool includeWL = true,
                    ulong guildId = 0,
                    ulong channelId = 0,
                    CommandContext? context = null)
    {
        if (_cts != null)
        {
            return MessageResources.OperationAlredyInProgressMessage;
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
            string messageContent = MessageResources.AccessingMembersListMessage;
            var builder = new DiscordMessageBuilder()
                .WithContent(messageContent)
                .AddActionRowComponent(InteractionsHelper.GetCancelUpdateRankButton());
            WriteToConsole(messageContent);
            message = await context.EditResponseAsync(builder);
        }

        var result = await _communityDataService.UpdatePlayersAsync(
            includeWL,
            async (s, e) =>
            {
                message = await UpdateMessageAsync(e, channelId, message);
            },
            minutesUpdateThreshold: minutesUpdateThreshold,
            cancellationToken: _cts.Token);

        if (result == -1 || _cts?.Token.IsCancellationRequested == true)
        {
            await UpdateMessageAsync(MessageResources.OperationCancelledMessage, channelId, message);
            _cts = null;
            return MessageResources.OperationCancelledMessage;
        }
        else
        {
            await UpdateMessageAsync(string.Format(MessageResources.UpdatedProfilesCountFormat, result.ToString()), channelId, message, true);
        }
        _cts = null;
        return null;
    }

    private static void WriteToConsole(string message, ConsoleColor foregroundColor = ConsoleColor.White)
    {
        ConsoleInteractionsHelper.WriteLine(message, foregroundColor);
    }

    private async Task<DiscordMessage?> UpdateMessageAsync(string content, ulong channelId, DiscordMessage? message = null, bool removeComponents = false)
    {
        try
        {
            WriteToConsole(content);
            if (message == null)
            {
                var channel = await _botService.Client.GetChannelAsync(channelId);
                return await channel.SendMessageAsync(content);
            }
            if (removeComponents)
            {
                var builder = new DiscordMessageBuilder()
                    .WithContent(content); // No components added => existing components cleared
                return await message.ModifyAsync(builder);
            }

            return await message.ModifyAsync(content);
        }
        catch (Exception e)
        {
            LogNotifier.Notify(e.Message);
            return null;
        }
    }
}