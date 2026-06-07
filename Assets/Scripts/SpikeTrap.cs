using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpikeTrap : NetworkBehaviour
{
    [Networked] int visTeam { get; set; }

    [SerializeField] List<MeshRenderer> mesh;

    public void SetVisableTeam(int num)
    {
        visTeam = num;

        StartCoroutine(CloakDelay());
    }

    void OnTriggerEnter(Collider other)
    {
        if (!Object.HasStateAuthority) return;

        Player target = other.GetComponentInParent<Player>();

        if (target)
        {
            Vector3 knockPos = target.transform.position - transform.position;

            target.ApplyHit(transform.position, 25, knockPos, 2, 5);
            Rpc_RequestVisible(target.team);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void Rpc_RequestInvisible()
    {
        Debug.Log("In Rpc");

        NetworkObject netPlayer = Runner.GetPlayerObject(Runner.LocalPlayer);

        if (netPlayer && netPlayer.TryGetComponent<Player>(out Player target))
        {
            Debug.Log(target);

            if (target.team != visTeam)
            {
                foreach (MeshRenderer m in mesh)
                    m.enabled = false;
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void Rpc_RequestVisible(int num)
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

    IEnumerator CloakDelay()
    {
        Debug.Log("Coroutine");
        yield return new WaitForSeconds(1f);

        if (Object.HasStateAuthority)
            Rpc_RequestInvisible();
    }
}