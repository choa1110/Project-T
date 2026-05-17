using UnityEngine;
using Fusion;

public class BuffItem : NetworkBehaviour
{
    [SerializeField] NetworkObject _networkObj;

    [SerializeField] int buffNum;
    [SerializeField] int buffRank;

    void OnTriggerEnter(Collider other)
    {
        if (!Object.HasStateAuthority) return;

        if (other.TryGetComponent<BuffSystem>(out BuffSystem sys))
        {
            sys.Rpc_BroadcastApplyBuff(buffRank, buffNum);
            Runner.Despawn(_networkObj);
        }
    }
}