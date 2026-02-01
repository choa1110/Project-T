using UnityEngine;

public class ItemBox : MonoBehaviour
{
    public ItemDB _db;

    void OnTriggerEnter(Collider other)
    {
        ItemSystem sys = other.GetComponent<ItemSystem>();

        if (sys != null)
        {
            int num = Random.Range(0, _db.itemList.Count);

            if (sys.SetItem(_db.itemList[num]))
                Destroy(gameObject);
        }
    }
}