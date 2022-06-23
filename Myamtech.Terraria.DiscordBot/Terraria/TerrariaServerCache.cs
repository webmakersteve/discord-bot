using System.Collections;
using System.Collections.Immutable;

namespace Myamtech.Terraria.DiscordBot.Terraria;

public class TerrariaServerCache : IEnumerable<TerrariaServerCache.Entry>
{
    
    public event EventHandler<Entry>? OnUpdate;
    
    public sealed class Entry : IEquatable<Entry>
    {
        public string WorldName { get; }
        public ImmutableList<ApiTypes.Player> Players { get; }
        public int MaxPlayers { get; }
        public int Port { get; }

        public Entry(
            string worldName,
            ImmutableList<ApiTypes.Player> players,
            int maxPlayers,
            int port
        )
        {
            WorldName = worldName;
            Players = players;
            MaxPlayers = maxPlayers;
            Port = port;
        }

        public bool Equals(Entry? other)
        {
            if (other == null)
            {
                return false;
            }

            if (other.Players.Count != Players.Count)
            {
                return false;
            }

            if (
                !string.Equals(WorldName, other.WorldName, StringComparison.Ordinal) ||
                Port != other.Port ||
                MaxPlayers != other.MaxPlayers
            )
            {
                return false;
            }

            // These should be pre-sorted
            for (int i = 0; i < other.Players.Count; i++)
            {
                if (other.Players[i].Username != Players[i].Username)
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            // We're going to use a hash code 
            int playersHashCode = 0;

            foreach (var player in Players)
            {
                playersHashCode ^= player.Username.GetHashCode();
            }

            return HashCode.Combine(
                WorldName,
                MaxPlayers,
                Port,
                playersHashCode
            );
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Entry);
        }
    }

    private sealed class PlayerComparer : IComparer<ApiTypes.Player>
    {
        public int Compare(ApiTypes.Player? x, ApiTypes.Player? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (ReferenceEquals(null, y)) return 1;
            if (ReferenceEquals(null, x)) return -1;
            var nicknameComparison = string.Compare(x.Nickname, y.Nickname, StringComparison.Ordinal);
            if (nicknameComparison != 0) return nicknameComparison;
            return string.Compare(x.Username, y.Username, StringComparison.Ordinal);
        }
    }

    public ImmutableDictionary<string, Entry> Servers { get; private set; } = ImmutableDictionary<string, Entry>.Empty;

    public Entry Update(string worldName, ApiTypes.ServerStatusV2 status)
    {
        var entry = new Entry(
            worldName, 
            status.Player.Where(x => !string.IsNullOrEmpty(x.Username)).ToImmutableList().Sort(new PlayerComparer()),
            status.MaxPlayers, 
            status.Port
        );
        
        Servers = Servers.SetItem(worldName, entry);
        
        OnUpdate?.Invoke(this, entry);

        return entry;
    }

    public IEnumerator<Entry> GetEnumerator()
    {
        return Servers.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}