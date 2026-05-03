using UnityEngine;
using Fusion;

public class ItemBox : NetworkBehaviour
{
    NetworkObject _networkObj;

    [SerializeField] int from = 0, to = 0;

    void Awake()
    {
        _networkObj = GetComponent<NetworkObject>();
    }

    public void SetItemRange(int start, int end)
    {
        from = start; to = end;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!Object.HasStateAuthority)
            return;

        Player target = other.GetComponent<Player>();

        if (target != null)
        {
            if (target.item.SetItem(ItemDB.Instance.GetItem(Random.Range(from, to))))
                Runner.Despawn(_networkObj);
        }
    }
}