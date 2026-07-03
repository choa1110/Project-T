using UnityEngine;
using Fusion;

public class ItemBox : NetworkBehaviour
{
    [SerializeField] NetworkObject _networkObj;

    [SerializeField] int from, to;

    public void SetItemRange(int start, int end)
    {
        from = start; to = end;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!Object.HasStateAuthority) return;

        if (other.TryGetComponent<ItemSystem>(out ItemSystem sys))
        {
            if (sys.SetItem(ItemDB.Instance.GetItem(Random.Range(from, to))))
                Runner.Despawn(_networkObj);
        }
    }
}