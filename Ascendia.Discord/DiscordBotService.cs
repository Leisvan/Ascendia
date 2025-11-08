using Ascendia.Core.Interactivity;
using Ascendia.Core.Models;
using Ascendia.Core.Services;
using DSharpPlus;
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

    public async Task<bool> ConnectAsync()
    {
        try
        {
            ConsoleInteractionsHelper.ClearConsole();
            await _client.ConnectAsync();
            return true;
        }
        catch (Exception e)
        {
            ConsoleInteractionsHelper.WriteErrorLine(e.Message);
            return false;
        }
    }

    public async Task DisconnectAsync()
    {
        try
        {
            await _client.DisconnectAsync();
            ConsoleInteractionsHelper.ClearConsole();
            ConsoleInteractionsHelper.WriteLine("Bot desconectado");
        }
        catch (Exception e)
        {
            ConsoleInteractionsHelper.WriteErrorLine(e.Message);
        }
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
}