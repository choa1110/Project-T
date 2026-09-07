using System.Collections.Generic;
using UnityEngine;
using Fusion;



#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 맵 프리팹 루트에 붙이는 스크립트.
/// 플레이어 스폰 지점과 아이템 드랍 지점을 지정합니다.
///
/// 사용법:
/// 1. 맵 프리팹에 NetworkObject 컴포넌트 추가
/// 2. 맵 프리팹에 이 MapInfo 컴포넌트 추가
/// 3. Inspector에서 자식 빈 오브젝트들을 스폰 지점으로 지정
/// </summary>
public class MapInfo : NetworkBehaviour
{
    [Header("플레이어 스폰 지점 (인덱스 순서로 플레이어에게 할당됨)")]
    [SerializeField] List<Transform> playerSpawnPoints = new List<Transform>();

    [Header("아이템 드랍 지점 (랜덤으로 하나 선택됨)")]
    [SerializeField] List<ItemPoint> itemDropPoints = new List<ItemPoint>();

    public int PlayerSpawnCount => playerSpawnPoints.Count;
    public int ItemDropCount => itemDropPoints.Count;

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_BroadcastToggleMap(bool trigger)
    {
        gameObject.SetActive(trigger);

        if (trigger)
            ShufflePoints();
    }

    /// <summary>
    /// 플레이어 인덱스에 해당하는 스폰 Transform 반환.
    /// 스폰 지점 수보다 인덱스가 크면 순환 적용됩니다.
    /// </summary>
    public Transform GetPlayerSpawnPoint(int playerIndex)
    {
        if (playerSpawnPoints == null || playerSpawnPoints.Count == 0)
        {
            Debug.LogWarning($"[MapInfo] '{gameObject.name}'에 플레이어 스폰 지점이 없습니다.");
            return null;
        }

        return playerSpawnPoints[playerIndex % playerSpawnPoints.Count];
    }

    public void ShufflePoints()
    {
        for (int i = playerSpawnPoints.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);

            // 두 요소의 위치 교환 (Swap)
            Transform temp = playerSpawnPoints[i];
            playerSpawnPoints[i] = playerSpawnPoints[randomIndex];
            playerSpawnPoints[randomIndex] = temp;
        }
    }

    public ItemPoint GetRandomItemDropPosition()
    {
        List<ItemPoint> tmp = new List<ItemPoint>();

        foreach (ItemPoint point in itemDropPoints)
        {
            if (!point.isOccupied)
                tmp.Add(point);
        }

        if (tmp != null && tmp.Count > 0)
        {
            ItemPoint point = tmp[Random.Range(0, tmp.Count)];

            return point;
        }
        else
            return null;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        DrawGizmosInternal();
    }

    void DrawGizmosInternal()
    {
        // 플레이어 스폰 지점 - 파란색 구체 + 전방 방향
        if (playerSpawnPoints != null)
        {
            for (int i = 0; i < playerSpawnPoints.Count; i++)
            {
                if (playerSpawnPoints[i] == null) continue;
                Vector3 pos = playerSpawnPoints[i].position;

                Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.9f);
                Gizmos.DrawSphere(pos, 0.4f);
                Gizmos.DrawLine(pos, pos + playerSpawnPoints[i].forward * 1.2f);

                Handles.Label(pos + Vector3.up * 0.9f,
                    $"P{i} 스폰",
                    new GUIStyle { normal = { textColor = new Color(0.3f, 0.7f, 1f) }, fontStyle = FontStyle.Bold });
            }
        }

        // 아이템 드랍 지점 - 노란색 와이어 박스
        if (itemDropPoints != null)
        {
            Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.9f);
            for (int i = 0; i < itemDropPoints.Count; i++)
            {
                if (itemDropPoints[i] == null) continue;
                Vector3 pos = itemDropPoints[i].pos;

                Gizmos.DrawWireCube(pos, new Vector3(1.5f, 0.1f, 1.5f));
                Gizmos.DrawSphere(pos, 0.25f);

                Handles.Label(pos + Vector3.up * 0.6f,
                    $"아이템{i}",
                    new GUIStyle { normal = { textColor = new Color(1f, 0.85f, 0.1f) }, fontStyle = FontStyle.Bold });
            }
        }
    }
#endif
}
