using Ascendia.Core.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ascendia.Core.Models;

public record class GuildSettingsModel(DiscordBotGuildSettingsRecord Record)
{
    private ulong? _guildId;

    public ulong GuildId => _guildId ??= Record.GetIdNumber();

    public string RankingChannelId => Record?.RankingChannelId ?? string.Empty;

    public int RegionUpdateThresholdInMinutes => Record?.RegionUpdateThresholdInMinutes ?? 0;

    public override string ToString() => Record?.GuildName ?? "-";
}