using UnityEngine;
using Fusion;

public class Barrier : NetworkBehaviour
{
    [SerializeField] Player owner;

    [SerializeField] float _duration = 7f;

    [Networked] float curDur { get; set; }

    void OnEnable()
    {
        ResetDuration();
    }

    public override void FixedUpdateNetwork()
    {
        curDur -= Runner.DeltaTime;

        if (curDur < 0 && Object.HasStateAuthority)
            Rpc_BroadcastDeactivateBarrier();
    }

    public void ResetDuration()
    {
        curDur = _duration;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void Rpc_BroadcastDeactivateBarrier()
    {
        gameObject.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!Object.HasStateAuthority) return;

        Player target = other.GetComponentInParent<Player>();

        if (target && target != owner)
        {
            Debug.Log(other);
            Vector3 knockDir = target.transform.position - transform.position;

            target.ApplyHit(transform.position, 0, knockDir, 10, 5);
        }
    }
}