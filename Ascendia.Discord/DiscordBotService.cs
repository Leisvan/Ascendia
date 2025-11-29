using Ascendia.Core;
using Ascendia.Core.Models;
using Ascendia.Core.Services;
using Ascendia.Discord.Internal;
using Ascendia.Discord.Strings;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;

namespace Ascendia.Discord;

public class DiscordBotService
{
    private readonly DiscordClient _client;
    private readonly CommunityService _communityService;
    private readonly GuildActionsService _guildActions;

    public DiscordBotService(DiscordClient client, CommunityService communityService, LadderService ladderService, InteractivityExtension interactivity)
    {
        _client = client;
        _communityService = communityService;
        _guildActions = new GuildActionsService(communityService, this, ladderService, interactivity);
    }

    public DiscordClient Client => _client;

    internal GuildActionsService Actions => _guildActions;

    public void CancelOperation()
        => _guildActions.CancelOperation();

    public async Task<bool> ConnectAsync()
    {
        try
        {
            await _client.ConnectAsync();
            CoreTelemetry.WriteSuccessLine(Messages.BotConnected);
            return true;
        }
        catch (Exception e)
        {
            CoreTelemetry.WriteErrorLine(e.Message);
            return false;
        }
    }

    public async Task DisconnectAsync()
    {
        try
        {
            await _client.DisconnectAsync();
            CoreTelemetry.WriteSuccessLine(Messages.BotDisconnected);
        }
        catch (Exception e)
        {
            CoreTelemetry.WriteErrorLine(e.Message);
        }
    }

    public async Task DisplayRankingAsync(bool includeBanned, ulong guildId = 0)
    {
        var guildSettings = _communityService.GetGuildSettings(guildId);
        if (guildSettings == null || !ulong.TryParse(guildSettings.RankingChannelId, out ulong channelId) || channelId == 0)
        {
            var errorMessage = Messages.ChannelIdNotFoundError;
            CoreTelemetry.WriteErrorLine(errorMessage);
            return;
        }
        await _guildActions.DisplayRankingAsync(includeBanned, channelId);
    }

    public async Task<List<GuildSettingsModel>?> GetSettingServersAsync(bool includeDebugGuilds, bool forceRefresh = false)
    {
        var members = await _communityService.GetAllGuildSettingsAsync(forceRefresh);
        if (!includeDebugGuilds)
        {
            members = members?.Where(x => !x.Record.IsDebugGuild).ToList();
        }
        return members;
    }

    public async Task<bool> UpdateMembersLadderAsync(
        bool forceUpdate = false, 
        bool includeWL = true, 
        bool notify = true, 
        bool nullAs80 = true,
        ulong guildId = 0)
    {
        var guildSettings = _communityService.GetGuildSettings(guildId);
        if (guildSettings == null || !ulong.TryParse(guildSettings.RankingChannelId, out ulong channelId) || channelId == 0)
        {
            var errorMessage = Messages.ChannelIdNotFoundError;
            CoreTelemetry.WriteErrorLine(errorMessage);
            return false;
        }
        var errorResult = await _guildActions.UpdateMembersLadderAsync(forceUpdate, includeWL, notify, nullAs80, guildId, channelId);
        return errorResult == null;
    }

    internal async Task RespondToInteractionAsync(ComponentInteractionCreatedEventArgs args)
    {
        if (args.Id == InteractionsHelper.CancelRegionUpdateButtonId)
        {
            CancelOperation();
            var builder = new DiscordInteractionResponseBuilder()
                .WithContent(Messages.OperationCancelling);

            await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage, builder);
        }
    }
}