using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;

namespace CreepyUtil.Archipelago.ApClient;

public partial class ApClient
{
    public HashSet<Hint> Hints { get; set; } = [];
    
    private Hint[] WaitingHints = [];

    /// <summary>
    /// Update Hints
    ///
    /// and for anything in the future
    /// </summary>
    public bool PushUpdatedVariables(bool updateHintArr, out Hint[] hints)
    {
        var hasAnythingChanged = false;
        if (HintsAwaitingUpdate)
        {
            if (updateHintArr)
            {
                Hints = WaitingHints.OrderHints(PlayerNames.Length, PlayerSlotArr);
            }

            hasAnythingChanged = true;
            HintsAwaitingUpdate = false;
        }

        hints = WaitingHints;
        return hasAnythingChanged;
    }
    
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