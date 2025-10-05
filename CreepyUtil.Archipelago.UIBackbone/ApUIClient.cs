using System.Numerics;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using CreepyUtil.BackBone;
using ImGuiNET;
using static Archipelago.MultiClient.Net.Enums.HintStatus;
using static Archipelago.MultiClient.Net.Enums.ItemFlags;
using Color = Raylib_cs.Color;

namespace CreepyUtil.Archipelago.UIBackbone;

public class ApUIClient : ApClient.ApClient
{
    public static Vector4 White = Color.White.ToV4();
    public static Vector4 DirtyWhite = new(0.95f, 0.95f, 0.81f, 1f);
    public static Vector4 Red = Color.Red.ToV4();
    public static Vector4 Green = Color.Green.ToV4();
    public static Vector4 Gold = Color.Gold.ToV4();
    public static Vector4 Blue = Color.Blue.ToV4();
    public static Vector4 ChillBlue = new(0.39f, 0.58f, 0.91f, 1f);
    public static Vector4 SkyBlue = Color.SkyBlue.ToV4();
    public static Vector4 Purple = Color.Purple.ToV4();
    public static Vector4 DarkPurple = new(0.89f, 0.01f, 0.89f, 1f);

    public Func<int, bool> IsPlayer;

    public Dictionary<HintStatus, string> HintStatusText =
        Enum.GetValues<HintStatus>().ToDictionary(hs => hs, hs => Enum.GetName(hs)!);

    public Dictionary<HintStatus, Vector4> HintStatusColor = new()
    {
        [Found] = Green,
        [Unspecified] = White,
        [NoPriority] = SkyBlue,
        [Avoid] = Red,
        [Priority] = Purple,
    };

    public event EventHandler<string> OnDeathLinkReceived;
    public event EventHandler<MessagePacket> OnChatMessagePacket;
    public event EventHandler<MessagePart[]> OnItemLogPacket;

    public ApUIClient(bool connectPacket = true)
    {
        IsPlayer = slot => slot == PlayerSlot;
        if (!connectPacket) return;
        // OnConnectionEvent += client =>
        //     client.Session.Socket.PacketReceived += OnPacketReceived;
    }

    public void OnPacketReceived(ArchipelagoPacketBase packet)
    {
        switch (packet)
        {
            case ChatPrintJsonPacket message:
                OnChatMessagePacket(this, new ChatMessagePacket(this, IsPlayer, message));
                break;
            case BouncedPacket bouncedPacket:
                if (!bouncedPacket.Tags.Contains("DeathLink")) return;
                var source = bouncedPacket.Data.TryGetValue("source", out var sourceToken)
                    ? sourceToken.ToString()
                    : "Unknown";
                Console.WriteLine(source);
                OnDeathLinkReceived(this, source);
                break;
            case PrintJsonPacket updatePacket:
                if (updatePacket.Data.Length == 1)
                {
                    OnChatMessagePacket(this, new ServerMessagePacket(updatePacket.Data.First().Text!));
                }

                if (updatePacket.Data.Length < 2) break;
                if (updatePacket.Data.First().Text!.StartsWith("[Hint]: "))
                {
                    if (updatePacket.Data.Last().HintStatus!.Value == Found) break;
                    OnChatMessagePacket(this, new HintMessagePacket(this, IsPlayer, updatePacket.Data));
                }
                else if (updatePacket.Data[1].Text is " found their " or " sent ")
                {
                    OnItemLogPacket(this, updatePacket.Data.Select(mp => new MessagePart(this, IsPlayer, mp)).ToArray());
                }

                break;
        }
    }

    public Vector4 GetItemColor(ItemFlags item)
    {
        if (item.HasFlag(Advancement)) return Gold;
        if (item.HasFlag(Trap)) return Red;
        return item.HasFlag(NeverExclude) ? Blue : SkyBlue;
    }

    public Vector4 GetItemColor(ScoutedItemInfo item)
    {
        if (item.ItemName.Contains(" Shield")) return Red;
        if (item.Flags.HasFlag(Advancement)) return Gold;
        if (item.Flags.HasFlag(Trap) || item.Flags.HasFlag(NeverExclude)) return Blue;
        return SkyBlue;
    }

    public void PrintPlayerName(int slot)
        => ImGui.TextColored(
            slot == PlayerSlot ? DarkPurple : slot == 0 ? Gold : DirtyWhite, PlayerNames[slot]);
}