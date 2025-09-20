using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.DataPackage;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;

namespace CreepyUtil.Archipelago.Overrides;

public class ItemReceiver : IReceivedItemsHelper
{
    public readonly IArchipelagoSocketHelper socket;
    public readonly ILocationCheckHelper locationsHelper;
    public readonly IItemInfoResolver itemInfoResolver;
    public readonly IConnectionInfoProvider connectionInfoProvider;
    public readonly IPlayerHelper playerHelper;

    ConcurrentQueue<ItemInfo> itemQueue;

    readonly IConcurrentList<ItemInfo> allItemsReceived;

    ReadOnlyCollection<ItemInfo> cachedReceivedItems;

    /// <inheritdoc/>
    public int Index => cachedReceivedItems.Count;

    /// <inheritdoc/>
    public ReadOnlyCollection<ItemInfo> AllItemsReceived => cachedReceivedItems;

    /// <inheritdoc/>
    // public delegate void ItemReceiverHandler(ItemReceiverHandler helper);

    /// <inheritdoc/>
    public event ReceivedItemsHelper.ItemReceivedHandler ItemReceived;

    internal ItemReceiver(
        IArchipelagoSocketHelper socket, ILocationCheckHelper locationsHelper,
        IItemInfoResolver itemInfoResolver, IConnectionInfoProvider connectionInfoProvider,
        IPlayerHelper playerHelper)
    {
        this.socket = socket;
        this.locationsHelper = locationsHelper;
        this.itemInfoResolver = itemInfoResolver;
        this.connectionInfoProvider = connectionInfoProvider;
        this.playerHelper = playerHelper;

        itemQueue = new ConcurrentQueue<ItemInfo>();
        allItemsReceived = new ConcurrentList<ItemInfo>();
        cachedReceivedItems = allItemsReceived.AsReadOnlyCollection();

        socket.PacketReceived += Socket_PacketReceived;
    }

    /// <inheritdoc/>
    public bool Any() => !itemQueue.IsEmpty;

    /// <inheritdoc/>
    public ItemInfo PeekItem()
    {
        itemQueue.TryPeek(out var item);
        return item;
    }

    /// <inheritdoc/>
    public ItemInfo DequeueItem()
    {
        itemQueue.TryDequeue(out var item);
        return item;
    }

    /// <inheritdoc/>
    public string GetItemName(long id, string game = null) => itemInfoResolver.GetItemName(id, game);

    void Socket_PacketReceived(ArchipelagoPacketBase packet)
    {
        switch (packet.PacketType)
        {
            case ArchipelagoPacketType.ReceivedItems:
            {
                var receivedItemsPacket = (ReceivedItemsPacket)packet;

                if (receivedItemsPacket.Index == 0)
                {
                    PerformResynchronization(receivedItemsPacket);
                    break;
                }

                if (allItemsReceived.Count != receivedItemsPacket.Index)
                {
                    socket.SendPacket(new SyncPacket());
                    locationsHelper.CompleteLocationChecks();
                    break;
                }

                foreach (var networkItem in receivedItemsPacket.Items)
                {
                    var sender = playerHelper.GetPlayerInfo(networkItem.Player) ?? new PlayerInfo();
                    var item = new ItemInfo(networkItem, connectionInfoProvider.Game, sender.Game, itemInfoResolver,
                        sender);

                    allItemsReceived.Add(item);
                    itemQueue.Enqueue(item);

                    cachedReceivedItems = allItemsReceived.AsReadOnlyCollection();

                    // ItemReceived?.Invoke(this);
                }

                break;
            }
        }
    }

    void PerformResynchronization(ReceivedItemsPacket receivedItemsPacket)
    {
        var previouslyReceived = allItemsReceived.AsReadOnlyCollection();

        itemQueue = new ConcurrentQueue<ItemInfo>();
        allItemsReceived.Clear();

        foreach (var networkItem in receivedItemsPacket.Items)
        {
            var sender = playerHelper.GetPlayerInfo(networkItem.Player) ?? new PlayerInfo();
            var item = new ItemInfo(networkItem, connectionInfoProvider.Game, sender.Game, itemInfoResolver, sender);

            itemQueue.Enqueue(item);
            allItemsReceived.Add(item);

            cachedReceivedItems = allItemsReceived.AsReadOnlyCollection();

            // if (ItemReceived != null && !previouslyReceived.Contains(item))
            //     ItemReceived(this);
        }
    }
}