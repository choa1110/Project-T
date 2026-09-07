using UnityEngine;
using Fusion;

/// <summary>
/// 맵 기믹 공통 베이스. 트리거 영역에 플레이어가 닿으면 효과 발동.
/// 판정은 StateAuthority에서만 수행해 네트워크 일관성을 보장한다.
/// 시각 효과(머티리얼 등)는 각 파생 클래스에서 로컬로 처리.
/// </summary>
[RequireComponent(typeof(Collider))]
public abstract class MapGimmickBase : NetworkBehaviour
{
    [Header("기믹 공통 설정")]
    [SerializeField] protected bool requireTrigger = true;

    protected virtual void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponentInParent<Player>();
        if (player == null) return;
        OnPlayerEnter(player);
    }

    void OnTriggerStay(Collider other)
    {
        var player = other.GetComponentInParent<Player>();
        if (player == null) return;
        OnPlayerStay(player);
    }

    void OnTriggerExit(Collider other)
    {
        var player = other.GetComponentInParent<Player>();
        if (player == null) return;
        OnPlayerExit(player);
    }

    protected virtual void OnPlayerEnter(Player player) { }
    protected virtual void OnPlayerStay(Player player) { }
    protected virtual void OnPlayerExit(Player player) { }
}
