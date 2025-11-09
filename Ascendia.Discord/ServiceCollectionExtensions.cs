using Ascendia.Discord.Commands;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Extensions;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Ascendia.Discord
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureDiscordClient(
                   this IServiceCollection services, string? token)
        {
            ArgumentNullException.ThrowIfNull(token);

            services
                .AddDiscordClient(token, DiscordIntents.Guilds | DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents | SlashCommandProcessor.RequiredIntents)
                .AddCommandsExtension((serviceProvider, extension) =>
                {
                    extension.AddCommands([typeof(PlayersCommand)]);
                })
                .AddInteractivityExtension()
                .ConfigureEventHandlers(b => b.AddEventHandlers<ComponentInteractionCreatedEventHandler>(ServiceLifetime.Singleton));

            return services;
        }
    }
}