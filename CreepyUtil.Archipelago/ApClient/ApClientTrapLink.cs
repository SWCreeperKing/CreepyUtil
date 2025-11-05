namespace CreepyUtil.Archipelago.ApClient;

public partial class ApClient
{
    private Dictionary<string, Action<string>> TrapLink = [];
    public event Action<string, string>? OnUnregisteredTrapLinkReceived;
    
    public void SendTrapLink(string trap)
    {
        if (!Tags[ArchipelagoTag.TrapLink]) throw new ArgumentException("Cannot send traplink if client does not have the traplink tag");
        new Task(() =>
            {
                lock (Session!)
                {
                    Session?.Socket.SendPacketAsync(PacketMaker.CreateTrapLinkPacket(PlayerName, trap));
                }
            })
           .RunWithTimeout(ServerTimeout);
    }
    
    /// <summary>
    /// Possible traps:
    /// https://docs.google.com/spreadsheets/d/1yoNilAzT5pSU9c2hYK7f2wHAe9GiWDiHFZz8eMe1oeQ/edit?pli=1&gid=811965759#gid=811965759
    ///
    /// <see cref="ApClient.OnUnregisteredTrapLinkReceived"/> must have a listener before adding
    /// </summary>
    /// <param name="trapName">Name of the trap</param>
    /// <param name="trapAction">Action for the trap, name of sender is passed</param>
    public void AddTrapLinkTrap(string trapName, Action<string> trapAction)
    {
        if (OnUnregisteredTrapLinkReceived is null)
            throw new ArgumentException(
                "There is no event on [OnUnregisteredTrapLinkReceived], please add one before adding specific traps");

        TrapLink[trapName] = trapAction;
    }
}