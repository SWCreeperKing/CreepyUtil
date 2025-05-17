using Archipelago.MultiClient.Net.Packets;

namespace CreepyUtil.Archipelago.Commands;

public interface IApCommandInterface
{
    public string Command { get; set; }
    public int MinArgumentLength { get; set; }

    public void RunCommand(ApClient client, ChatPrintJsonPacket message, string[] args);
}