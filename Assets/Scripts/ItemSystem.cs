using System.Collections.Generic;
using UnityEngine;

public class ItemSystem : MonoBehaviour
{
    Item[] itemList = new Item[2];

    [SerializeField] List<ItemSlot> slotList;

    public bool SetItem(Item item)
    {
        bool isRoom = false;

        for (int i = 0; i < 2; i++)
        {
            if (itemList[i] == null)
            {
                itemList[i] = item;
                slotList[i].InputSlot(item.itemIcon);
                isRoom = true;

                break;
            }
        }

        return isRoom;
    }

    public void UseItem(Player user, int num)
    {
        if (itemList[num] == null) return;

        ItemDB.Instance.UseItem(itemList[num].itemId, user);
        itemList[num] = null;

        slotList[num].EmptySlot();
    }
}