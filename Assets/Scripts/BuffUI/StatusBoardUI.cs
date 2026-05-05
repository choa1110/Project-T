using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Fusion;

/// <summary>
/// Tab 키 스코어보드 매니저.
/// Tab 누르는 동안 보드 표시, 떼면 숨김.
///
/// 씬 설정:
///   1. Canvas 아래에 이 컴포넌트 배치
///   2. boardPanel    : 전체 패널 (기본 비활성)
///   3. listContainer : 행들이 들어갈 Vertical Layout Group
///   4. rowPrefab     : PlayerStatusRow 컴포넌트가 붙은 프리팹
/// </summary>
public class StatusBoardUI : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] GameObject boardPanel;
    [SerializeField] Transform  listContainer;
    [SerializeField] GameObject rowPrefab;

    [Header("설정")]
    [SerializeField] bool holdToShow = true;   // true=누르는 동안 표시, false=토글

    bool _visible;

    void Start()
    {
        if (boardPanel != null) boardPanel.SetActive(false);
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        bool tabDown = Keyboard.current.tabKey.wasPressedThisFrame;
        bool tabUp   = Keyboard.current.tabKey.wasReleasedThisFrame;

        if (holdToShow)
        {
            if (tabDown) SetVisible(true);
            if (tabUp)   SetVisible(false);
        }
        else
        {
            if (tabDown) SetVisible(!_visible);
        }
    }

    void SetVisible(bool show)
    {
        _visible = show;
        if (boardPanel != null) boardPanel.SetActive(show);
        if (show) PopulateRows(forceRebuild: true);
    }

    void PopulateRows(bool forceRebuild)
    {
        if (!forceRebuild || listContainer == null || rowPrefab == null) return;

        foreach (Transform child in listContainer) Destroy(child.gameObject);

        var allPlayers = new List<Player>(FindObjectsByType<Player>(FindObjectsSortMode.None));
        allPlayers.Sort((a, b) =>
        {
            int idA = (a.Object != null) ? a.Object.InputAuthority.PlayerId : 0;
            int idB = (b.Object != null) ? b.Object.InputAuthority.PlayerId : 0;
            return idA.CompareTo(idB);
        });

        NetworkRunner runner = FindFirstObjectByType<NetworkRunner>();

        foreach (var player in allPlayers)
        {
            if (player.Object == null) continue;

            int    pid    = player.Object.InputAuthority.PlayerId;
            bool   isSelf = player.Object.HasInputAuthority;
            string label  = $"Player {pid}{(isSelf ? "  (Me)" : "")}";

            float curHP = player.CurrentHP;
            float maxHP = player.stats.GetStat(StatType.MaxHP)?.Value ?? 1f;

            var buffSys   = player.GetComponent<BuffSystem>();
            var buffNames = buffSys != null ? buffSys.GetActiveBuffNames() : new List<string>();

            var rowGO = Instantiate(rowPrefab, listContainer);
            var row   = rowGO.GetComponent<PlayerStatusRow>();
            if (row != null)
                row.SetData(label, curHP, maxHP, buffNames, isSelf, player.team);
        }
    }
}
