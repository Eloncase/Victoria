using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace Victoria
{
    public static class DiscordExtensions
    {
        public static async Task<Lavalink> InitializeLavalinkAsync(this DiscordSocketClient socketClient)
        {
            var lavalink = new Lavalink();
            await lavalink.AddNodeAsync(socketClient).ConfigureAwait(false);
            return lavalink;
        }

        public static async Task<IReadOnlyDictionary<int, Lavalink>> InitializeLavalinkAsync(
            this DiscordShardedClient shardedClient)
        {
            var dictionary = new Dictionary<int, Lavalink>();

            foreach (var shard in shardedClient.Shards)
            {
                var lavalink = await InitializeLavalinkAsync(shard).ConfigureAwait(false);
                dictionary.Add(shard.ShardId, lavalink);
            }

            return dictionary;
        }
    }
}