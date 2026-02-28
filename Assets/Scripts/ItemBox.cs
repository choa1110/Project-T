using UnityEngine;

public class ItemBox : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        ItemSystem sys = other.GetComponent<ItemSystem>();

        if (sys != null)
        {
            if (sys.SetItem(ItemDB.Instance.SetRandomItem()))
                Destroy(gameObject);
        }
    }
}