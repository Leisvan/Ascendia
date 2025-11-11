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

    public static Task<string> GetRankEmojiStringAsync(DiscordClient client, int? tier)
    {
        var emojiId = tier switch
        {
            80 => EmojiResources.Rank_Immortal,
            _ => EmojiResources.Rank_Unranked,
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