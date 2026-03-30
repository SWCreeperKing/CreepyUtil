namespace CreepyUtil.Archipelago.ApClient;

public partial class ApClient
{
    /// <summary>
    /// Source, Message, Group
    /// </summary>
    public event Action<string, string, string>? OnDeathLinkPacketReceived;

    public HashSet<string> DeathLinkGroups = [""];

    private void SetupDeathLink()
    {
        OnBouncedPacketReceived += packet =>
        {
            if (!Tags[ArchipelagoTag.DeathLink]) return;
            var tags = packet.Tags.Where(tag => tag.StartsWith("DeathLink"))
                             .Select(tag => tag.Substring(9))
                             .Where(group => DeathLinkGroups.Contains(group))
                             .ToArray();
            
            if (tags.Length == 0) return;
            var source = (string)packet.Data["source"]!;
            var message = packet.Data.TryGetValue("cause", out var cause)
                ? (string)cause!
                : "Died a generic (unknown) death";
            if (source == PlayerName && ExcludeBouncedPacketsFromSelf) return;

            foreach (var group in tags) OnDeathLinkPacketReceived?.Invoke(group, source, message);
        };
    }

    public void SendDeathLink(string cause)
    {
        if (!Tags[ArchipelagoTag.DeathLink])
            throw new ArgumentException("Cannot send deathlink if client does not have the deathlink tag");
        new Task(() =>
            {
                lock (Session!)
                {
                    foreach (var group in DeathLinkGroups)
                    {
                        Session?.Socket.SendPacketAsync(PacketMaker.CreateDeathLinkPacket(group, PlayerName, cause));
                    }
                }
            })
           .RunWithTimeout(ServerTimeout, OnErrorReceived);
    }
}