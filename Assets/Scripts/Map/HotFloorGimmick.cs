using UnityEngine;
using Fusion;

/// <summary>
/// 주기적으로 달궈지는 데미지 바닥. 활성 상태일 때 위에 있는 플레이어에게 일정 간격으로 데미지.
/// 머티리얼 색/이미션으로 위험 상태를 시각 표시(로컬).
/// Map001(붉은 사막 콜로세움) 등에 사용.
/// </summary>
public class HotFloorGimmick : MapGimmickBase
{
    [Header("주기 설정 (초)")]
    [SerializeField] float safeDuration = 3f;
    [SerializeField] float warningDuration = 1f;
    [SerializeField] float hotDuration = 2f;

    [Header("데미지")]
    [SerializeField] float damagePerTick = 8f;
    [SerializeField] float damageInterval = 0.5f;

    [Header("시각 표시")]
    [SerializeField] Renderer floorRenderer;
    [SerializeField] Color safeColor = new Color(0.9f, 0.6f, 0.4f);
    [SerializeField] Color warnColor = new Color(1f, 0.8f, 0.2f);
    [SerializeField] Color hotColor = new Color(1f, 0.25f, 0.1f);

    [Networked] float Cycle { get; set; }
    [Networked] int Phase { get; set; }   // 0 safe, 1 warn, 2 hot

    float _tickTimer;
    int _lastVisualPhase = -1;
    Material _matInstance;

    public override void Spawned()
    {
        if (floorRenderer != null)
            _matInstance = floorRenderer.material;
        ApplyVisual(0);
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        Cycle += Runner.DeltaTime;
        float total = safeDuration + warningDuration + hotDuration;
        if (Cycle >= total) Cycle -= total;

        if (Cycle < safeDuration) Phase = 0;
        else if (Cycle < safeDuration + warningDuration) Phase = 1;
        else Phase = 2;
    }

    public override void Render()
    {
        if (Phase != _lastVisualPhase)
        {
            ApplyVisual(Phase);
            _lastVisualPhase = Phase;
        }
    }

    void ApplyVisual(int phase)
    {
        if (_matInstance == null) return;
        Color c = phase == 2 ? hotColor : (phase == 1 ? warnColor : safeColor);
        _matInstance.color = c;
    }

    protected override void OnPlayerStay(Player player)
    {
        if (!Object.HasStateAuthority) return;
        if (Phase != 2) return;
        if (player.IsDead) return;

        _tickTimer -= Runner.DeltaTime;
        if (_tickTimer <= 0f)
        {
            _tickTimer = damageInterval;
            player.ApplyHit(player.transform.position, damagePerTick, Vector3.up, 2f, 0.1f);
        }
    }
}
