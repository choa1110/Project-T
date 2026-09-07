using UnityEngine;
using Fusion;

[RequireComponent(typeof(Collider))]
public class BambooSegment : NetworkBehaviour
{
    [SerializeField] float regrowTime = 6f;
    [SerializeField] Renderer bambooRenderer;
    [SerializeField] Collider blockCollider;

    [Networked] public NetworkBool IsCut { get; set; }
    [Networked] float RegrowTimer { get; set; }

    bool _lastCutVisual;

    public override void Spawned()
    {
        ApplyVisual(IsCut);
        _lastCutVisual = IsCut;
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;
        if (IsCut)
        {
            RegrowTimer -= Runner.DeltaTime;
            if (RegrowTimer <= 0f)
                IsCut = false;
        }
    }

    public override void Render()
    {
        if (IsCut != _lastCutVisual)
        {
            ApplyVisual(IsCut);
            _lastCutVisual = IsCut;
        }
    }

    void ApplyVisual(bool cut)
    {
        if (bambooRenderer != null) bambooRenderer.enabled = !cut;
        if (blockCollider != null) blockCollider.enabled = !cut;
    }

    void OnTriggerStay(Collider other)
    {
        if (!Object.HasStateAuthority) return;
        if (IsCut) return;
        Player player = other.GetComponentInParent<Player>();
        if (player == null) return;
        if (player.IsDead) return;
        if (player.ComboStep > 0)
        {
            IsCut = true;
            RegrowTimer = regrowTime;
        }
    }
}
