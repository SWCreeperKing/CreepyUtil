using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;

namespace CreepyUtil.Archipelago.ApClient;

public partial class ApClient
{
    public void UpdateHint(int slot, long location, HintStatus priority)
    {
        Session?.Socket.SendPacketAsync(new UpdateHintPacket
                 {
                     Player = slot,
                     Location = location,
                     Status = priority
                 })
                .GetAwaiter()
                .GetResult();
    }
}