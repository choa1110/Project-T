using UnityEngine;
using Fusion;

public class ItemBox : NetworkBehaviour
{
    void OnTriggerEnter(Collider other)
    {

        if (Object == null || !Object.IsValid) return;
        
        if(!Object.HasStateAuthority) return;

        ItemSystem sys = other.GetComponent<ItemSystem>();

        if (sys != null)
        {
            int randomItemId = ItemDB.Instance.GetRandomItemID();
            
            if(sys.GiveItem(randomItemId))
            {
                Runner.Despawn(Object);
            }
        }
    }
} 