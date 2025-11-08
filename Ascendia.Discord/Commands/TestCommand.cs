using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Trees.Metadata;

namespace Ascendia.Discord.Commands;

[Command("test")]
[AllowedProcessors(typeof(SlashCommandProcessor))]
public class TestCommand
{
    [Command("message")]
    public async ValueTask Message(CommandContext context)
    {
        var guildId = context.Guild?.Id ?? 0;
        await context.RespondAsync("Some response");
    }
}