namespace CreepyUtil.Archipelago;

public readonly struct LoginInfo(int port, string slot, string address = "archipelago.gg", string password = "")
{
    public readonly string Address = address;
    public readonly string Password = password;
    public readonly string Slot = slot;
    public readonly int Port = port;

    public override string ToString() => $"Login Info: [{Address}:{Port}] with [{Password}] as [{Slot}]";

    public void Deconstruct(out string address, out string password, out string slot, out int port)
    {
        address = Address;
        password = Password;
        slot = Slot;
        port = Port;
    }
}