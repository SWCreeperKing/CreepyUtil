using Archipelago.MultiClient.Net.Packets;

namespace CreepyUtil.Archipelago.Commands;

public class ApCommandHandler(ApClient client)
{
    private ApClient Client = client;
    private Dictionary<string, IApCommandInterface> _Commands = [];

    public void RunCommand(ChatPrintJsonPacket message, string[] split)
    {
        if (!_Commands.TryGetValue(split[0].ToLower(), out var command))
        {
            Client.Say($"Command: [{split[0]}] does not exist");
            return;
        }

        if (split.Length - 1 < command.MinArgumentLength)
        {
            Client.Say($"Command: [{split[0]}] was not given the correct amount of arguments");
            return;
        }
        
        command.RunCommand(Client, message, split.Length == 0 ? [] : split.Skip(1).ToArray());
    }

    public void RegisterCommand(IApCommandInterface command) => _Commands.Add(command.Command, command);
    public void DeregisterCommand(IApCommandInterface command) => _Commands.Remove(command.Command);
}