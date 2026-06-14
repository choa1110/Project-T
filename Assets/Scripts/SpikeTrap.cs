using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpikeTrap : NetworkBehaviour
{
    [Networked] int ownerTeam { get; set; }

    [SerializeField] Collider hitCol;
    [SerializeField] List<MeshRenderer> mesh;

    public void SetTrap(int num)
    {
        ownerTeam = num;

        if (Object.HasStateAuthority)
            StartCoroutine(SetupDelay());
    }

    void OnTriggerEnter(Collider other)
    {
        if (!Object.HasStateAuthority) return;

        Player target = other.GetComponentInParent<Player>();

        if (target)
        {
            Vector3 knockPos = target.transform.position - transform.position;

            target.ApplyHit(transform.position, 20, knockPos, 2, 5);
            Rpc_BroadcastReveal(target.team);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void Rpc_BroadcastConceal()
    {
        NetworkObject netPlayer = Runner.GetPlayerObject(Runner.LocalPlayer);

        if (netPlayer && netPlayer.TryGetComponent<Player>(out Player target))
        {
            if (target.team != ownerTeam)
            {
                foreach (MeshRenderer m in mesh)
                    m.enabled = false;
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void Rpc_BroadcastReveal(int num)
    {
        NetworkObject netPlayer = Runner.GetPlayerObject(Runner.LocalPlayer);

        if (netPlayer && netPlayer.TryGetComponent<Player>(out Player target))
        {
            if (target.team == num)
            {
                foreach (MeshRenderer m in mesh)
                    m.enabled = true;
            }
        }
    }

    IEnumerator SetupDelay()
    {
        yield return new WaitForSeconds(1f);

        Rpc_BroadcastConceal();
        hitCol.enabled = true;
    }
}