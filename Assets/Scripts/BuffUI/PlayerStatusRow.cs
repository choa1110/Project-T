using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Tab 스코어보드 - 플레이어 1명의 행(Row).
/// 이름 | HP 바 | 버프 아이콘 목록 표시.
///
/// 프리팹 구조:
///   PlayerStatusRow
///     ├─ NameText        (TextMeshProUGUI)
///     ├─ HPBar           (Slider)
///     ├─ HPText          (TextMeshProUGUI)   ← "120 / 200"
///     └─ BuffIconContainer (HorizontalLayoutGroup)
///          └─ BuffIconPrefab (GameObject with Image)
/// </summary>
public class PlayerStatusRow : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] Slider          hpBar;
    [SerializeField] TextMeshProUGUI hpText;
    [SerializeField] Transform       buffIconContainer;
    [SerializeField] GameObject      buffIconPrefab;

    [Header("색상 - 본인 / 팀1 / 팀2")]
    [SerializeField] Color selfColor  = new Color(1.0f, 0.9f, 0.3f);
    [SerializeField] Color team1Color = new Color(0.4f, 0.8f, 1.0f);
    [SerializeField] Color team2Color = new Color(1.0f, 0.5f, 0.5f);

    /// <summary>
    /// 행 데이터 갱신.
    /// playerName  : 표시할 이름 (예: "Player 1 (Me)")
    /// currentHP   : 현재 HP
    /// maxHP       : 최대 HP
    /// buffNames   : 현재 적용 중인 버프 이름 리스트
    /// isSelf      : 로컬 플레이어 본인 여부
    /// teamIndex   : 0=팀없음/팀1, 1=팀2
    /// </summary>
    public void SetData(string playerName, float currentHP, float maxHP,
                        List<string> buffNames, bool isSelf, int teamIndex)
    {
        // 이름
        if (nameText != null)
        {
            nameText.text  = playerName;
            nameText.color = isSelf ? selfColor : teamIndex == 0 ? team1Color : team2Color;
        }

        // HP 바
        float ratio = maxHP > 0 ? currentHP / maxHP : 0f;
        if (hpBar  != null) hpBar.value = ratio;
        if (hpText != null) hpText.text = $"{Mathf.CeilToInt(currentHP)} / {Mathf.CeilToInt(maxHP)}";

        // 버프 아이콘
        RebuildBuffIcons(buffNames);
    }

    void RebuildBuffIcons(List<string> buffNames)
    {
        if (buffIconContainer == null || buffIconPrefab == null) return;

        foreach (Transform child in buffIconContainer) Destroy(child.gameObject);

        if (buffNames == null) return;
        foreach (var bName in buffNames)
        {
            var iconGO  = Instantiate(buffIconPrefab, buffIconContainer);
            var tooltip = iconGO.GetComponentInChildren<TextMeshProUGUI>();
            if (tooltip != null) tooltip.text = bName;
        }
    }
}
