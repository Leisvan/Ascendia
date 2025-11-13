using DSharpPlus;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ascendia.Discord
{
    public class ComponentInteractionCreatedEventHandler(DiscordBotService service) : IEventHandler<ComponentInteractionCreatedEventArgs>
    {
        private readonly DiscordBotService _service = service;

        public Task HandleEventAsync(DiscordClient sender, ComponentInteractionCreatedEventArgs eventArgs)
            => _service.RespondToInteractionAsync(eventArgs);
    }
}