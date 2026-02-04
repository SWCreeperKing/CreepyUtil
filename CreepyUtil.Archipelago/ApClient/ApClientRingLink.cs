namespace CreepyUtil.Archipelago.ApClient;

public partial class ApClient
{
    public event Action<string, int>? OnRingLinkPacketReceived;
    
    public void SendRingLink(int amount)
    {
        if (!Tags[ArchipelagoTag.RingLink]) throw new ArgumentException("Cannot send ringlink if client does not have the ringlink tag");
        new Task(() =>
            {
                lock (Session!)
                {
                    Session?.Socket.SendPacketAsync(PacketMaker.CreateRingLinkPacket(PlayerSlot, amount));
                }
            })
           .RunWithTimeout(ServerTimeout, OnErrorReceived);
    }
}