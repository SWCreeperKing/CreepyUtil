using System.Collections.ObjectModel;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;

namespace CreepyUtil.Archipelago;

public class ItemHandler(IReceivedItemsHelper items)
{
    public int ItemIndex { get; private set; }
    public ReadOnlyCollection<ItemInfo> Items => items.AllItemsReceived;

    public event Action<ItemInfo, int>? OnItemReceived;
    public event Action<ItemInfo[], int>? OnNewItemsReceived;

    internal void Update(bool invokeReceive = true)
    {
        if (ItemIndex >= Items.Count) return;
        var newItems = Items.Skip(ItemIndex).ToArray();
        if (invokeReceive && OnNewItemsReceived is not null) OnNewItemsReceived.Invoke(newItems, ItemIndex);
        if (invokeReceive && OnItemReceived is not null)
        {
            foreach (var item in newItems) OnItemReceived.Invoke(item, ItemIndex++);
        }
        else ItemIndex += newItems.Length;
    }

    public void SetItemIndex(int newIndex, bool reReceiveItems)
    {
        ItemIndex = newIndex;
        Update(reReceiveItems);
    }

    public IEnumerable<ItemInfo> GetItemsFrom(int fromIndex) => Items.Skip(fromIndex);

    public ItemInfo[] GetStartingItems()
        => Items.Where(item => item.Player.Slot is 0 && item.LocationName is "Server").ToArray();
    
    public ItemInfo[] GetCheatedItems()
        => Items.Where(item => item.LocationName is "Cheat Console").ToArray();
}