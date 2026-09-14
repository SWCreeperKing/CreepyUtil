using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;

namespace CreepyUtil.Archipelago.ApClient;

public partial class ApClient
{
    public Hint[] Hints;

    public int HintCost => Session?.RoomState?.HintCost ?? -1;
    public int HintCostPercent => Session?.RoomState?.HintCostPercentage ?? -1;
    public int HintPoints => Session?.RoomState?.HintPoints ?? -1;
    public int LocationCheckPoints => Session?.RoomState?.LocationCheckPoints ?? -1;

    public void UpdateHint(int slot, long location, HintStatus priority)
    {
        Session?.Socket.SendPacketAsync(new UpdateHintPacket { Player = slot, Location = location, Status = priority })
                .GetAwaiter()
                .GetResult();
    }
}