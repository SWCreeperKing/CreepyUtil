namespace CreepyUtil.Archipelago.ApClient;

public partial class ApClient
{
    public void SendDeathLink(string cause)
    {
        if (!Tags[ArchipelagoTag.DeathLink]) throw new ArgumentException("Cannot send deathlink if client does not have the deathlink tag");
        new Task(() =>
            {
                lock (Session!)
                {
                    Session?.Socket.SendPacketAsync(PacketMaker.CreateDeathLinkPacket(PlayerName, cause));
                }
            })
           .RunWithTimeout(ServerTimeout);
    }

}