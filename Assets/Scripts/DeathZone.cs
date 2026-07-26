using Fusion;
using UnityEngine;

public class DeathZone : NetworkBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (!Object.HasStateAuthority) return;

        Player target = other.GetComponentInParent<Player>();

        if (target)
            target.ApplyDeath();
    }
}