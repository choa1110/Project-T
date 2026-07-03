using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 스코어보드 한 행. 닉네임 + 팀컬러 + 현재 보유 버프 아이콘 목록 표시.
/// </summary>
public class ScoreboardRow : MonoBehaviour
{
    public Image teamColorBar;
    public TMP_Text nicknameText;
    public Transform buffIconContainer;   // 버프 아이콘들이 들어갈 HorizontalLayoutGroup
    public Image buffIconPrefab;          // 버프 아이콘 Image 프리팹 (단순 Image 오브젝트)
    public Image myPlayerHighlight;

    [HideInInspector] public Player linkedPlayer;

    BuffSystem _buffSystem;

    static readonly Color[] TC =
    {
        new Color(0f, 0.04f, 0.8f, 1f),
        new Color(0.87f, 0f, 0f, 1f),
        new Color(0f, 0.85f, 0f, 1f),
        new Color(1f, 1f, 1f, 1f),
    };

    readonly List<Image> _spawnedIcons = new List<Image>();

    public void Setup(Player p, bool isMine)
    {
        linkedPlayer = p;
        _buffSystem = p.GetComponent<BuffSystem>();

        if (nicknameText != null) nicknameText.text = p.NickName.ToString();

        int t = p.team;
        Color c = (t >= 0 && t < TC.Length) ? TC[t] : TC[3];
        if (teamColorBar != null) teamColorBar.color = c;
        if (myPlayerHighlight != null) myPlayerHighlight.gameObject.SetActive(isMine);
    }

    void LateUpdate()
    {
        if (_buffSystem == null || buffIconContainer == null || buffIconPrefab == null) return;

        var buffs = _buffSystem.GetActiveBuffs();
        RebuildIcons(buffs);
    }

    void RebuildIcons(List<Buff> buffs)
    {
        // 아이콘 수가 같으면 스프라이트만 교체
        if (_spawnedIcons.Count == buffs.Count)
        {
            for (int i = 0; i < buffs.Count; i++)
                _spawnedIcons[i].sprite = buffs[i].icon;
            return;
        }

        // 수 달라지면 전부 재생성
        foreach (var img in _spawnedIcons)
            if (img != null) Destroy(img.gameObject);
        _spawnedIcons.Clear();

        foreach (var buff in buffs)
        {
            var icon = Instantiate(buffIconPrefab, buffIconContainer);
            icon.sprite = buff.icon;
            icon.enabled = buff.icon != null;
            _spawnedIcons.Add(icon);
        }
    }
}
