using UnityEngine;
using Fusion;

/// <summary>
/// 낙사 구역. 맵 아래에 넓게 배치하면 플레이어가 떨어졌을 때 큰 데미지를 줘 사망 처리.
/// Map003(부유 유적) 등 가장자리 낙하 맵에 사용.
/// </summary>
public class KillZoneGimmick : MapGimmickBase
{
    [Header("낙사 설정")]
    [SerializeField] float fallDamage = 99999f;

    protected override void OnPlayerEnter(Player player)
    {
        if (!Object.HasStateAuthority) return;
        if (player.IsDead) return;

        // 기존 public ApplyHit 사용 (큰 데미지 → 사망)
        Vector3 hitPos = player.transform.position;
        player.ApplyHit(hitPos, fallDamage, Vector3.down, 0f, 0.2f);
    }
}
