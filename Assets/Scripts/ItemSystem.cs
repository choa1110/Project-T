using System.Collections.Generic;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class ItemSystem : NetworkBehaviour
{
    [Networked, Capacity(2)]
    public NetworkArray<int> NetworkedItems { get; }

    [SerializeField] List<ItemSlot> slotList;
    private int[] _prevItems = new int[] { -1, -1 };

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            NetworkedItems.Set(0, -1);
            NetworkedItems.Set(1, -1);
        }
        if (Object.HasInputAuthority)
        {
            slotList = new List<ItemSlot>(GameManager.Instance.itemSlots);
        }
    }

    public bool GiveItem(int itemId)
    {
        if(!Object.HasStateAuthority) return false;

        for(int i = 0; i < 2 ; i++)
        {
            if(NetworkedItems[i] == -1)
            {
                NetworkedItems.Set(i, itemId);
                return true;
            }
        }
        return false;
    }

    public override void Render()
    {
        if (Object.HasInputAuthority)
        {
            for(int i = 0; i<2; i++)
            {
                if(_prevItems[i] != NetworkedItems[i])
                {
                    _prevItems[i] = NetworkedItems[i];

                    if(NetworkedItems[i] != -1)
                    {
                        Item itemData = ItemDB.Instance.GetItemByID(NetworkedItems[i]);
                        slotList[i].InputSlot(itemData.itemIcon);
                    }
                    else
                    {
                        slotList[i].EmptySlot();
                    }
                }
            }
        }
    }

    public void RequestUseItem(int slotNum)
    {
        if(Object.HasInputAuthority && NetworkedItems[slotNum] != -1)
        {
            RPC_UseItem(slotNum);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_UseItem(int slotNum)
    {
        int itemId = NetworkedItems[slotNum];

        if (itemId != -1)
        {
            ItemDB.Instance.UseItem(itemId, GetComponent<Player>());
            
            NetworkedItems.Set(slotNum, -1);
        }
    }
}