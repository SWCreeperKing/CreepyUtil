using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Packets;

namespace CreepyUtil.Archipelago.ApClient;

public partial class ApClient
{
    public event Action<HintPrintJsonPacket>? OnHintPrintJsonPacketReceived;
    public event Action<GoalPrintJsonPacket>? OnGoalPrintJsonPacketReceived;
    public event Action<ChatPrintJsonPacket>? OnChatPrintPacketReceived;
    public event Action<BouncedPacket>? OnBouncedPacketReceived;
    public event Action<PrintJsonPacket>? OnPrintJsonPacketReceived;
    public event Action<ServerChatPrintJsonPacket>? OnServerMessagePacketReceived;
    public event Action<ItemPrintJsonPacket>? OnItemLogPacketReceived;
    public event Action<ItemCheatPrintJsonPacket>? OnItemCheatLogPacketReceived;
    public event Action<TagsChangedPrintJsonPacket>? OnTagsChangedLogPacketReceived;
    public event Action<JoinPrintJsonPacket>? OnJoinLogPacketReceived;
    public event Action<LeavePrintJsonPacket>? OnLeaveLogPacketReceived;
    public event Action<CommandResultPrintJsonPacket>? OnCommandResult;
    public event Action<LocationChecksPacket>? OnLocationsChecked;
    public event Action<RoomUpdatePacket>? OnRoomUpdatePacketReceived;
    public event Action<RoomInfoPacket>? OnRoomInfoPacketReceived;
    public event Action<StatusUpdatePacket>? OnStatusUpdatePacketReceived;

    private void OnPacketReceived(ArchipelagoPacketBase packet)
    {
        switch (packet)
        {
            case StatusUpdatePacket statusUpdatePacket: OnStatusUpdatePacketReceived?.Invoke(statusUpdatePacket); break;
            case RoomInfoPacket roomInfoPacket: OnRoomInfoPacketReceived?.Invoke(roomInfoPacket); break;
            case RoomUpdatePacket roomUpdatePacket: OnRoomUpdatePacketReceived?.Invoke(roomUpdatePacket); break;
            case LocationChecksPacket locationChecksPacket: OnLocationsChecked?.Invoke(locationChecksPacket); break;
            case CommandResultPrintJsonPacket commandResultPacket: OnCommandResult?.Invoke(commandResultPacket); break;
            case JoinPrintJsonPacket joinPacket: OnJoinLogPacketReceived?.Invoke(joinPacket); break;
            case LeavePrintJsonPacket leavePacket: OnLeaveLogPacketReceived?.Invoke(leavePacket); break;
            case TagsChangedPrintJsonPacket tagsPacket: OnTagsChangedLogPacketReceived?.Invoke(tagsPacket); break;
            case GoalPrintJsonPacket goalPacket: OnGoalPrintJsonPacketReceived?.Invoke(goalPacket); break;
            case ServerChatPrintJsonPacket serverPacket: OnServerMessagePacketReceived?.Invoke(serverPacket); break;
            case ItemPrintJsonPacket itemPacket: OnItemLogPacketReceived?.Invoke(itemPacket); break;
            case ItemCheatPrintJsonPacket itemPacket: OnItemCheatLogPacketReceived?.Invoke(itemPacket); break;
            case HintPrintJsonPacket hintPacket: OnHintPrintJsonPacketReceived?.Invoke(hintPacket); break;
            case ChatPrintJsonPacket message:
                OnChatPrintPacketReceived?.Invoke(message);

                if (message.Message.StartsWith($"@{PlayerName} "))
                {
                    CommandHandler?.RunCommand(
                        message,
                        message.Message.Replace($"@{PlayerName} ", "").Split(' ')
                    );
                }

                break;
            case BouncedPacket bouncedPacket:
                OnBouncedPacketReceived?.Invoke(bouncedPacket);

                if (bouncedPacket.Tags.Contains("TrapLink"))
                {
                    if (!Tags[ArchipelagoTag.TrapLink]) return;
                    var source = (string)bouncedPacket.Data["source"]!;
                    if (source == PlayerName && ExcludeBouncedPacketsFromSelf) return;
                    var trap = (string)bouncedPacket.Data["trap_name"]!;

                    if (TrapLink.ContainsKey(trap)) { TrapLink[trap](source); }
                    else { OnUnregisteredTrapLinkReceived?.Invoke(source, trap); }
                }

                if (bouncedPacket.Tags.Contains("RingLink"))
                {
                    if (!Tags[ArchipelagoTag.RingLink]) return;
                    var source = PlayerNames[(int)bouncedPacket.Data["source"]!];
                    if (source == PlayerName && ExcludeBouncedPacketsFromSelf) return;
                    var amount = (int)bouncedPacket.Data["amount"]!;
                    OnRingLinkPacketReceived?.Invoke(source, amount);
                }

                break;
            case PrintJsonPacket printPacket: OnPrintJsonPacketReceived?.Invoke(printPacket); break;
        }
    }
}