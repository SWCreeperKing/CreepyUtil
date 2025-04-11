using CreepyUtil.BackBone;

namespace CreepyUtil.Archipelago.UIBackbone;

public class ItemLog : Messenger<MessagePart[]>
{
    protected override void OnSentMessage(string message) { }

    protected override void RenderMessage(MessagePart[] message)
    {
        for (var i = 0; i < message.Length; i++)
        {
            message[i].Render(i < message.Length - 1);
        }
    }
}