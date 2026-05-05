using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 로비 맵 선택 패널 매니저.
/// 호스트만 맵 풀 편집 가능, 클라이언트는 읽기 전용.
/// LobbyUIController 에서 패널 열 때 Open(isHost) 를 호출합니다.
/// </summary>
public class LobbyMapSelectUI : MonoBehaviour
{
    [Header("레퍼런스")]
    [SerializeField] MapDB           mapDB;
    [SerializeField] Transform       mapListContainer;
    [SerializeField] GameObject      mapEntryPrefab;

    [Header("UI")]
    [SerializeField] TextMeshProUGUI poolCountText;
    [SerializeField] Button          startButton;
    [SerializeField] TextMeshProUGUI startButtonText;
    [SerializeField] GameObject      hostOnlyHint;   // 클라이언트에게 보여줄 안내 오브젝트

    bool _isHost;

    void Awake()
    {
        if (mapDB == null) mapDB = MapDB.Instance;
    }

    /// <summary>
    /// 패널 열 때 LobbyUIController 에서 호출.
    /// isHost=true 면 맵 풀 편집 가능, false 면 읽기 전용.
    /// </summary>
    public void Open(bool isHost)
    {
        _isHost = isHost;
        if (mapDB == null) mapDB = MapDB.Instance;
        if (hostOnlyHint != null) hostOnlyHint.SetActive(!isHost);
        BuildMapList(rebuild: true);
    }

    void BuildMapList(bool rebuild)
    {
        if (!rebuild || mapDB == null || mapListContainer == null || mapEntryPrefab == null) return;

        foreach (Transform child in mapListContainer) Destroy(child.gameObject);

        foreach (var md in mapDB.AllMaps)
        {
            var go = Instantiate(mapEntryPrefab, mapListContainer);
            var ui = go.GetComponent<MapEntryUI>();
            if (ui != null) ui.Initialize(md, mapDB, _isHost, OnPoolChanged);
        }

        SyncUI(countOnly: false);
    }

    void OnPoolChanged()
    {
        SyncUI(countOnly: false);
    }

    void SyncUI(bool countOnly)
    {
        // 카운트 라벨
        if (poolCountText != null)
        {
            int n = mapDB != null ? mapDB.PoolCount : 0;
            poolCountText.text = n == 0 ? "선택된 맵 없음" : $"선택된 맵: {n}개";
        }

        // 시작 버튼
        if (startButton == null) return;
        bool ok = _isHost && mapDB != null && mapDB.PoolCount > 0;
        startButton.interactable = ok;
        if (startButtonText == null) return;
        if (!_isHost)  startButtonText.text = "호스트 대기 중...";
        else if (!ok)  startButtonText.text = "맵을 선택하세요";
        else           startButtonText.text = $"게임 시작 ({mapDB.PoolCount}개 맵)";
    }
}
