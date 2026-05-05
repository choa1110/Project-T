using System.Collections.Generic;
using Fusion;

public class ItemSystem : NetworkBehaviour
{
    [Networked, Capacity(2)]
    NetworkArray<int> ItemIDs => default;

    Item[] _localItems = new Item[2];
    List<ItemSlot> slotList = new List<ItemSlot>();

    public void HUDLink()
    {
        for (int i = 0; i < 2; i++)
            slotList.Add(HUDManager.Instance.itemSlots[i]);
    }

    public bool SetItem(Item item)
    {
        if (!Object.HasStateAuthority) return false;

        for (int i = 0; i < 2; i++)
        {
            if (ItemIDs[i] == 0)
            {
                ItemIDs.Set(i, item.itemId);

                return true;
            }
        }

        return false;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void Rpc_RequestUseItem(int num, Player user)
    {
        if (ItemIDs[num] == 0) return;

        ItemDB.Instance.UseItem(ItemIDs[num], user);
        ItemIDs.Set(num, 0);
    }

    public override void Render()
    {
        if (!Object.HasInputAuthority) return;

        for (int i = 0; i < 2; i++)
        {
            int currentId = ItemIDs[i];

            if (_localItems[i] == null || _localItems[i].itemId != currentId)
                UpdateSlotUI(i, currentId);
        }
    }

    void UpdateSlotUI(int index, int id)
    {
        if (id == 0)
        {
            _localItems[index] = null;
            slotList[index].EmptySlot();
        }
        else
        {
            Item newItem = ItemDB.Instance.GetItem(id - 1);
            _localItems[index] = newItem;
            slotList[index].InputSlot(newItem.itemIcon);
        }
    }
}