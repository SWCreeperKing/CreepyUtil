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
    public new abstract int GetHashCode();
}

public class ChatMessagePacket(ApUIClient client, Func<int, bool> isPlayer, ChatPrintJsonPacket packet) : MessagePacket
{
    private readonly ApUIClient Client = client;
    private readonly ChatPrintJsonPacket Packet = packet;
    private readonly string Message = $": {packet.Message}";
    private readonly int HashCode = $"{client.PlayerNames[packet.Slot]}: {packet.Message}".GetHashCode();

    public override void RenderMessage()
    {
        ImGui.TextColored(isPlayer(Packet.Slot) ? DarkPurple : DirtyWhite, Client.PlayerNames[Packet.Slot]);
        ImGui.SameLine(0, 0);
        ImGui.Text(Message);
    }

    public override int GetHashCode() => HashCode;
}

public class ServerMessagePacket(string message) : MessagePacket
{
    public readonly string Message = $": {message}";
    public readonly int HashCode = $"Server: {message}".GetHashCode();

    public override void RenderMessage()
    {
        ImGui.TextColored(Gold, "Server");
        ImGui.SameLine(0, 0);
        ImGui.Text(Message);
    }

    public override int GetHashCode() => HashCode;
}

public class HintMessagePacket(ApUIClient client, Func<int, bool> isPlayer, JsonMessagePart[] messageParts)
    : MessagePacket
{
    public readonly MessagePart[] MessageParts =
        messageParts.Select(mp => new MessagePart(client, isPlayer, mp)).ToArray();

    public readonly int HashCode = string.Join(" ", messageParts.Select(mp => mp.Text)).GetHashCode();

    public override void RenderMessage()
    {
        for (var i = 0; i < MessageParts.Length; i++)
        {
            MessageParts[i].Render(i < MessageParts.Length - 1);
        }
    }

    public override int GetHashCode() => HashCode;
}

public readonly struct MessagePart
{
    private readonly Vector4 Color = White;
    public readonly string Text = "";
    public readonly bool IsPlayerPart = false;
    public readonly Func<int, bool> IsPlayer;
    public readonly int Slot;

    public MessagePart(ApUIClient client, Func<int, bool> isPlayer, JsonMessagePart jsonMessagePart,
        Vector4? colorOverride = null)
    {
        switch (jsonMessagePart.Type)
        {
            case JsonMessagePartType.PlayerId:
                Slot = int.Parse(jsonMessagePart.Text);
                Color = isPlayer(Slot) ? DarkPurple : Slot == 0 ? Gold : DirtyWhite;
                Text = client.PlayerNames[Slot];
                IsPlayerPart = true;
                IsPlayer = isPlayer; 
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
        ImGui.TextColored(GetColor(), Text);
        if (!appendSameLine) return;
        ImGui.SameLine(0, 0);
    }

    public Vector4 GetColor()
    {
        if (IsPlayerPart) return IsPlayer(Slot) ? DarkPurple : Slot == 0 ? Gold : DirtyWhite;
        return Color;
    }
}