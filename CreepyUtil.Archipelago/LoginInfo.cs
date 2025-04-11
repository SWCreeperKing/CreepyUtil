namespace CreepyUtil.Archipelago;

public readonly struct LoginInfo(int port, string slot, string address = "archipelago.gg", string password = "")
{
    public readonly string Address = address;
    public readonly string Password = password;
    public readonly string Slot = slot;
    public readonly int Port = port;
}