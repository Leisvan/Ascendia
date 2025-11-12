using Ascendia.Discord.Strings;
using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ascendia.Discord.Internal;

internal class EmojisHelper
{
    private const string DefaultFlagEmojiString = ":globe_with_meridians:";

    private static readonly Dictionary<string, string> CacheMap = [];

    public static async Task<DiscordEmoji?> GetDiscordEmojiAsync(DiscordClient client, string emojiId)
    {
        if (ulong.TryParse(emojiId, out ulong id))
        {
            return await client.GetApplicationEmojiAsync(id);
        }
        return null;
    }

    public static Task<string> GetPositionEmojiStringAsync(DiscordClient client, int? position)
    {
        var emojiId = position switch
        {
            1 => EmojiResources.Position_Safelane_1,
            2 => EmojiResources.Position_Midlane_2,
            3 => EmojiResources.Position_Offlane_3,
            4 => EmojiResources.Position_Softsupport_4,
            5 => EmojiResources.Position_Hardsupport_5,
            _ => EmojiResources.Position_Unknown,
        };
        return GetEmojiStringAsync(client, emojiId);
    }

    public static Task<string> GetRankEmojiStringAsync(DiscordClient client, int? tier)
    {
        var emojiId = tier switch
        {
            80 => EmojiResources.Rank_8,
            >= 70 and <= 79 => EmojiResources.Rank_7,
            >= 60 and <= 69 => EmojiResources.Rank_6,
            >= 50 and <= 59 => EmojiResources.Rank_5,
            >= 40 and <= 49 => EmojiResources.Rank_4,
            >= 30 and <= 39 => EmojiResources.Rank_3,
            >= 20 and <= 29 => EmojiResources.Rank_2,
            >= 10 and <= 19 => EmojiResources.Rank_1,
            _ => EmojiResources.Rank_0,
        };
        return GetEmojiStringAsync(client, emojiId);
    }

    private static async Task<string> GetEmojiStringAsync(DiscordClient client, string emojiId)
    {
        if (CacheMap.TryGetValue(emojiId, out var value))
        {
            return value;
        }
        var emoji = await GetDiscordEmojiAsync(client, emojiId);
        if (emoji is not null)
        {
            var messageFormat = emoji.ToString();
            CacheMap.TryAdd(emojiId, messageFormat);
            return messageFormat;
        }
        return string.Empty;
    }
}