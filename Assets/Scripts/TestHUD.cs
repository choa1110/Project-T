using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 테스트용 HUD - 라운드/타이머/HP/버프 목록을 표시합니다.
/// Canvas 아래에 배치하고 인스펙터에서 각 필드를 연결해주세요.
/// </summary>
public class TestHUD : MonoBehaviour
{
    [Header("라운드 & 타이머")]
    public TMP_Text roundText;
    public TMP_Text timerText;

    [Header("HP 바")]
    public Image hpFill;
    public TMP_Text hpText;

    [Header("버프 목록")]
    public TMP_Text buffListText;

    Player _localPlayer;
    BuffSystem _buffSystem;
    PlayerStats _stats;

    void Update()
    {
        TryFindLocalPlayer();
        UpdateRoundTimer();
        UpdateHP();
        UpdateBuffList();
    }

    void TryFindLocalPlayer()
    {
        if (_localPlayer != null) return;

        foreach (var p in FindObjectsByType<Player>(FindObjectsSortMode.None))
        {
            if (p.Object != null && p.Object.HasInputAuthority)
            {
                _localPlayer = p;
                _buffSystem = p.GetComponent<BuffSystem>();
                _stats = p.stats;
                break;
            }
        }
    }

    void UpdateRoundTimer()
    {
        if (GameManager.Instance == null || !GameManager.Instance.Object.IsValid)
            return;

        if (roundText != null)
            roundText.text = $"Round {GameManager.Instance.CurrentRound}";

        if (timerText != null)
        {
            float t = Mathf.Max(0, GameManager.Instance.RoundTimer);
            int min = Mathf.FloorToInt(t / 60f);
            int sec = Mathf.FloorToInt(t % 60f);
            timerText.text = $"{min}:{sec:00}";
        }
    }

    void UpdateHP()
    {
        if (_localPlayer == null || _stats == null) return;

        float maxHP = _stats.GetStat(StatType.MaxHP).Value;
        float curHP = _localPlayer.CurrentHP;
        float rate = maxHP > 0 ? curHP / maxHP : 0f;

        if (hpFill != null)
            hpFill.fillAmount = rate;

        if (hpText != null)
            hpText.text = $"{Mathf.CeilToInt(curHP)} / {Mathf.CeilToInt(maxHP)}";
    }

    void UpdateBuffList()
    {
        if (buffListText == null) return;

        if (_buffSystem == null)
        {
            buffListText.text = "버프 없음";
            return;
        }

        List<string> names = _buffSystem.GetActiveBuffNames();
        buffListText.text = names.Count == 0 ? "버프 없음" : string.Join("\n", names);
    }
}
