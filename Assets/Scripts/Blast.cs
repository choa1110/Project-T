using Fusion;
using System.Collections;
using UnityEngine;

public class Blast : NetworkBehaviour
{
    Player owner;

    [SerializeField] ParticleSystem blastEffect;
    [SerializeField] AudioSource blastSound;
    [SerializeField] Collider col;

    float pow;
    float knock;

    public void SetOwner(Player player)
    {
        owner = player;
    }

    public void SetBlastStrength(float powDam, float powKnock)
    {
        pow = powDam;
        knock = powKnock;

        Rpc_StartBlast();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void Rpc_StartBlast()
    {
        col.enabled = true;

        blastEffect.Play();
        blastSound.Play();

        StartCoroutine(BlastTime());
    }

    void OnTriggerEnter(Collider other)
    {
        if (!Object.HasStateAuthority) return;

        Player target = other.GetComponentInParent<Player>();

        if (target)
        {
            Vector3 targetPos = target.transform.position;
            targetPos.y += 0.85f;
            Vector3 knockPos = targetPos - transform.position;

            if (target.team != owner.team && !target.barrier.activeSelf)
                target.ApplyHit(owner, transform.position, pow * 2f, knockPos, knock * 1.5f, 10);
            else
                target.ApplyHit(transform.position, 0, knockPos, knock, 7);
        }
    }

    IEnumerator BlastTime()
    {
        yield return new WaitForSeconds(0.3f);

        col.enabled = false;
    }
}