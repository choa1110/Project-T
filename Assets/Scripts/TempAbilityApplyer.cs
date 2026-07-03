using UnityEngine;
using Fusion;

public class TempAbilityApplyer : NetworkBehaviour
{
    [SerializeField] NetworkObject _networkObj;

    public Ability ability;

    void OnTriggerEnter(Collider other)
    {
        if (!Object.HasStateAuthority) return;

        if (other.TryGetComponent<Player>(out Player sys))
        {
            sys.Rpc_RequestSetAbility(ability.skillNum);
            Runner.Despawn(_networkObj);
        }
    }
}