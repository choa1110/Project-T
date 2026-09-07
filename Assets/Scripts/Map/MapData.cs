using Fusion;
using UnityEngine;

/// <summary>
/// 맵 하나에 대한 데이터 에셋.
/// Project 창에서 우클릭 → Create → Map → MapData 로 생성합니다.
/// </summary>
[CreateAssetMenu(fileName = "NewMapData", menuName = "Map/MapData")]
public class MapData : ScriptableObject
{
    [Header("맵 정보")]
    public string mapName = "새 맵";
    [TextArea(2, 4)]
    public string description;
    public Sprite thumbnail;

    [Header("네트워크 프리팹 (Fusion NetworkObject 필수)")]
    public NetworkPrefabRef mapPrefab;
}
