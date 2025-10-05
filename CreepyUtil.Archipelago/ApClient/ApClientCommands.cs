using CreepyUtil.Archipelago.Commands;

namespace CreepyUtil.Archipelago.ApClient;

public partial class ApClient
{
    private ApCommandHandler CommandHandler;
    
    public bool Say(string message)
    {
        return IsConnected && new Task(() =>
        {
            lock (Session!)
            {
                Session.Say(message);
            }
        }).RunWithTimeout(ServerTimeout);
    }
    
    public void RegisterCommand(IApCommandInterface command) => CommandHandler.RegisterCommand(command);
    public void DeregisterCommand(IApCommandInterface command) => CommandHandler.DeregisterCommand(command);
}