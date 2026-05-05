using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전체 맵 목록과 현재 라운드에서 선택될 맵 풀을 관리하는 DB.
/// 로비 씬에 GameObject를 만들고 이 컴포넌트를 붙이면 됩니다.
/// DontDestroyOnLoad로 게임 씬까지 유지됩니다.
/// </summary>
public class MapDB : MonoBehaviour
{
    static MapDB _instance;
    public static MapDB Instance => _instance;

    [Header("전체 맵 목록 (모든 맵 에셋 등록)")]
    [SerializeField] List<MapData> allMaps = new List<MapData>();

    [Header("현재 게임에서 소환될 맵 풀 (로비에서 설정)")]
    [SerializeField] List<MapData> mapPool = new List<MapData>();

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public IReadOnlyList<MapData> AllMaps => allMaps;
    public IReadOnlyList<MapData> MapPool => mapPool;
    public int PoolCount => mapPool.Count;

    /// <summary>
    /// 맵 풀에서 랜덤으로 맵 하나를 반환합니다.
    /// 풀이 비어있으면 전체 목록에서 랜덤으로 선택합니다.
    /// </summary>
    public MapData GetRandomMap()
    {
        if (mapPool.Count > 0)
            return mapPool[Random.Range(0, mapPool.Count)];

        if (allMaps.Count > 0)
        {
            Debug.LogWarning("[MapDB] 맵 풀이 비어있어 전체 목록에서 랜덤 선택합니다.");
            return allMaps[Random.Range(0, allMaps.Count)];
        }

        Debug.LogError("[MapDB] 등록된 맵이 없습니다! allMaps에 MapData를 추가해주세요.");
        return null;
    }

    public void AddToPool(MapData map)
    {
        if (map == null || mapPool.Contains(map)) return;
        mapPool.Add(map);
        Debug.Log($"[MapDB] '{map.mapName}' 풀에 추가. 현재 풀 크기: {mapPool.Count}");
    }

    public void RemoveFromPool(MapData map)
    {
        if (mapPool.Remove(map))
            Debug.Log($"[MapDB] '{map.mapName}' 풀에서 제거. 현재 풀 크기: {mapPool.Count}");
    }

    public bool IsInPool(MapData map) => map != null && mapPool.Contains(map);
}
