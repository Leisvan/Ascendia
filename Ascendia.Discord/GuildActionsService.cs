using Ascendia.Core.Services;
using DSharpPlus.Interactivity;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ascendia.Discord;

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
    private CancellationTokenSource? _updateLadderTokenSource;
}