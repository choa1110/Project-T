using UnityEngine;
using Fusion;

public class ItemBox : NetworkBehaviour
{
    NetworkObject _networkObj;

    [SerializeField] int from = 0;
    [SerializeField] int to = 0;

    void Awake()
    {
        _networkObj = GetComponent<NetworkObject>();
    }

    void OnTriggerEnter(Collider other)
    {
        Player target = other.GetComponent<Player>();

        if (target == null) return;

        Rpc_RequestSetItemToServer(target);

        //if (Object.HasStateAuthority)
        //{
        //    Debug.Log("Helllooooo");
        //    ItemSystem sys = other.GetComponent<ItemSystem>();
        //
        //    if (sys != null)
        //    {
        //        if (sys.SetItem(ItemDB.Instance.GetRandomItem(from, to)))
        //            Runner.Despawn(_networkObj);
        //    }
        //}
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    void Rpc_RequestSetItemToServer(Player target)
    {
        if (target.item.SetItem(ItemDB.Instance.GetRandomItem(from, to)))
            Runner.Despawn(_networkObj);
    }
}