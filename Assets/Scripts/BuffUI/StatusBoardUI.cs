using UnityEngine;
using Fusion; // Fusion 네임스페이스 필요

public class StatusBoardUI : MonoBehaviour
{
    [Header("UI Components")]
    public GameObject boardPanel;       // 껐다 켰다 할 전체 패널
    public Transform listContainer;     // Row들이 들어갈 Vertical Layout 그룹
    public GameObject rowPrefab;        // 위에서 만든 PlayerStatusRow 프리팹

    void Start()
    {
        boardPanel.SetActive(false); // 시작할 땐 꺼두기
    }

    void Update()
    {
        // 탭 키로 토글 (InputSystem을 쓰시지만 UI는 Legacy Input이 편할 때가 많습니다)
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            bool show = !boardPanel.activeSelf;
            boardPanel.SetActive(show);

            if (show) RefreshBoard();
        }
    }

    void RefreshBoard()
    {
        // 1. 기존 목록 초기화
        foreach (Transform child in listContainer)
            Destroy(child.gameObject);

        // 2. 씬에 있는 모든 플레이어(BuffSystem) 찾기
        // (Fusion의 경우 FindObjectsOfType으로 찾아도 되고, Runner.ActivePlayers를 써도 됨)
        var allPlayers = FindObjectsOfType<BuffSystem>();

        foreach (var buffSys in allPlayers)
        {
            // 플레이어 객체가 아직 초기화 안 됐으면 패스
            if (buffSys.Object == null) continue;

            // Row 생성
            GameObject rowObj = Instantiate(rowPrefab, listContainer);
            PlayerStatusRow row = rowObj.GetComponent<PlayerStatusRow>();

            // 이름 결정 (Player ID 또는 닉네임)
            string pName = $"Player {buffSys.Object.InputAuthority.PlayerId}";

            // 나 자신(Local Player)인지 표시해주면 좋음
            if (buffSys.Object.HasInputAuthority)
                pName += " (Me)";

            // 3. Phase 2에서 만든 GetSyncBuffs()로 버프 목록 가져오기!
            row.SetInfo(pName, buffSys.GetSyncBuffs());
        }
    }
}