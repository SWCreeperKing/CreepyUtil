using CreepyUtil.Archipelago.PackReader.Types;

namespace CreepyUtil.Archipelago.PackReader.Bridge;

public class AutoTracker() // someone can implement this, i don't *think* its needed for ap
{
    public Func<Enums.AutoTrackingBackendName, Enums.AutoTrackingState> ConnectionState =
        _ => Enums.AutoTrackingState.Unavailable;

    public dynamic SelectedConnectorType;
    public int ReadU8(int baseAddr, int offset = 0) => throw new NotImplementedException();
    public int ReadU16(int baseAddr, int offset = 0) => throw new NotImplementedException();
    public int ReadU24(int baseAddr, int offset = 0) => throw new NotImplementedException();
    public int ReadU32(int baseAddr, int offset = 0) => throw new NotImplementedException();

    //labeled as deprecated, docs say to use `store:ReadVariable`
    public object? ReadVariable(string variableName) => throw new NotImplementedException();

    public int GetConnectionState(string backend) => (int)ConnectionState(backend.TextToAutoTrackingBackendName());
}