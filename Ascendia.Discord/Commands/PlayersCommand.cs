using Ascendia.Discord.Strings;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Trees.Metadata;
using DSharpPlus.Entities;

namespace Ascendia.Discord.Commands;

[Command("players")]
[AllowedProcessors(typeof(SlashCommandProcessor))]
public class PlayersCommand(DiscordBotService service)
{
    private readonly DiscordBotService _service = service;

    [Command("rank")]
    [RequirePermissions(DiscordPermission.Administrator)]
    public async ValueTask RankAsync(CommandContext context, bool includeBanned = false)
    {
        var guildId = context.Guild?.Id ?? 0;

        await context.RespondAsync(Messages.StartingOperation);

        var result = await _service.Actions.DisplayRankingAsync(includeBanned, context: context);
        if (result != null)
        {
            await context.EditResponseAsync(result);
        }
    }

    [Command("update")]
    [RequirePermissions(DiscordPermission.Administrator)]
    public async ValueTask UpdateAsync(CommandContext context, bool forceUpdate = true, bool incudeWL = true)
    {
        var guildId = context.Guild?.Id ?? 0;

        await context.RespondAsync(Messages.StartingOperation);

        var result = await _service.Actions.UpdateMembersLadderAsync(forceUpdate, incudeWL, true, guildId, context.Channel.Id, context);
        if (result != null)
        {
            await context.EditResponseAsync(result);
        }
    }
}