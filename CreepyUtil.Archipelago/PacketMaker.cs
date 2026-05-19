using Archipelago.MultiClient.Net.Converters;
using Archipelago.MultiClient.Net.Packets;
using Newtonsoft.Json.Linq;

namespace CreepyUtil.Archipelago;

public static class PacketMaker
{
    public static BouncePacket CreateDeathLinkPacket(HashSet<string> groups, string playerName, string cause) => new()
    {
        Tags = groups.Select(g => $"DeathLink{g}").ToList(),
        Data = new Dictionary<string, JToken>
        {
            { "time", DateTime.UtcNow.ToUnixTimeStamp() },
            { "source", playerName },
            { "cause", cause }
        }
    };
    
    public static BouncePacket CreateTrapLinkPacket(string playerName, string trapName) => new()
    {
        Tags = ["TrapLink"],
        Data = new Dictionary<string, JToken>
        {
            { "time", DateTime.UtcNow.ToUnixTimeStamp() },
            { "source", playerName },
            { "trap_name", trapName }
        }
    };
    
    public static BouncePacket CreateRingLinkPacket(int playerSlot, int amount) => new()
    {
        Tags = ["RingLink"],
        Data = new Dictionary<string, JToken>
        {
            { "time", DateTime.UtcNow.ToUnixTimeStamp() },
            { "source", playerSlot },
            { "amount", amount }
        }
    };
}