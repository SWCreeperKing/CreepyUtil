using System.Numerics;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using CreepyUtil.BackBone;
using ImGuiNET;
using static CreepyUtil.Archipelago.UIBackbone.ApUIClient;

namespace CreepyUtil.Archipelago.UIBackbone;

public class TextClient : Messenger<MessagePacket>
{
    public event EventHandler<string> OnMessageSend;
    
    protected override void OnSentMessage(string message) => OnMessageSend(this, message);
    protected override void RenderMessage(MessagePacket message) => message.RenderMessage();
}

public abstract class MessagePacket
{
    public abstract void RenderMessage();
    // public abstract string GetMessage();
    // public abstract long GetTimeStamp();
}

public class ChatMessagePacket(ApUIClient client, ChatPrintJsonPacket packet) : MessagePacket
{
    private readonly ApUIClient Client = client;
    private readonly ChatPrintJsonPacket Packet = packet;
    private readonly string Message = $": {packet.Message}";
    // private readonly string FullMessage = $"{client.PlayerNames[packet.Slot]}: {packet.Message}";
    // public readonly long Timestamp = packet.

    public override void RenderMessage()
    {
        Client.PrintPlayerName(Packet.Slot);
        ImGui.SameLine(0, 0);
        ImGui.Text(Message);
    }

    // public override string GetMessage()
    // {
    //     
    // }
    //
    // public override long GetTimeStamp()
    // {
    //     
    // }
}

public class ServerMessagePacket(string message) : MessagePacket
{
    public readonly string Message = $": {message}";

    public override void RenderMessage()
    {
        ImGui.TextColored(Gold, "Server");
        ImGui.SameLine(0, 0);
        ImGui.Text(Message);
    }
}

public class HintMessagePacket(ApUIClient client, JsonMessagePart[] messageParts) : MessagePacket
{
    public readonly MessagePart[] MessageParts = messageParts.Select(mp => new MessagePart(client, mp)).ToArray();

    public override void RenderMessage()
    {
        for (var i = 0; i < MessageParts.Length; i++)
        {
            MessageParts[i].Render(i < MessageParts.Length - 1);
        }
    }
}

public readonly struct MessagePart
{
    public readonly Vector4 Color = White;
    public readonly string Text = "";

    public MessagePart(ApUIClient client, JsonMessagePart jsonMessagePart, Vector4? colorOverride = null)
    {
        switch (jsonMessagePart.Type)
        {
            case JsonMessagePartType.PlayerId:
                var slot = int.Parse(jsonMessagePart.Text);
                Color = slot == client.PlayerSlot ? DarkPurple : slot == 0 ? Gold : DirtyWhite;
                Text = client.PlayerNames[slot];
                break;
            case JsonMessagePartType.ItemId:
                Color = client.GetItemColor(jsonMessagePart.Flags!.Value);
                Text = client.ItemIdToItemName(long.Parse(jsonMessagePart.Text), jsonMessagePart.Player!.Value);
                break;
            case JsonMessagePartType.LocationId:
                Color = Green;
                Text = client.LocationIdToLocationName(long.Parse(jsonMessagePart.Text), jsonMessagePart.Player!.Value);
                break;
            case JsonMessagePartType.EntranceName:
                var entranceName = jsonMessagePart.Text.Trim();
                Color = entranceName == "" ? White : ChillBlue;
                Text = entranceName == "" ? "Vanilla" : entranceName;
                break;
            case JsonMessagePartType.HintStatus:
                Color = client.HintStatusColor[(HintStatus)jsonMessagePart.HintStatus!];
                Text = client.HintStatusText[(HintStatus)jsonMessagePart.HintStatus!];
                break;
            default:
                Text = jsonMessagePart.Text ?? "";
                break;
        }

        if (colorOverride is null) return;
        Color = colorOverride.Value;
    }

    public void Render(bool appendSameLine = true)
    {
        ImGui.TextColored(Color, Text);
        if (!appendSameLine) return;
        ImGui.SameLine(0, 0);
    }
}