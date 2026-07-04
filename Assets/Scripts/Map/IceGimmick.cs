using UnityEngine;
using Fusion;

/// <summary>
/// 빙판 미끄럼 기믹. 트리거 영역 위에 있는 플레이어를 미끄럼 상태로 만든다.
/// Player.SetSlippery()를 매 프레임 갱신해 영역 안에서만 미끄럽게.
/// Map002(얼음 행성)에 사용.
/// </summary>
public class IceGimmick : MapGimmickBase
{
    [Header("미끄럼 설정")]
    [SerializeField] float slipRefresh = 0.3f;  // 매 Stay마다 갱신할 미끄럼 지속시간

    protected override void OnPlayerStay(Player player)
    {
        if (!Object.HasStateAuthority) return;
        if (player.IsDead) return;

        player.SetSlippery(slipRefresh);
    }
}
