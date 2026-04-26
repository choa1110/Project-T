using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{
    static HUDManager _instance;
    public static HUDManager Instance { get => _instance; }

    public Image charPortrait;
    public TMP_Text charName;
    public PercentageFillBar hpBar;

    public List<ItemSlot> itemSlots;

    public SkillInterface skillInterface;

    public List<OpponentData> opponentDatas;

    void Awake()
    {
        if (_instance == null)
            _instance = this;
    }

    public void LinkOpponent(Player opponent, int num)
    {
        opponentDatas[num].gameObject.SetActive(true);
        opponentDatas[num].SetOpponentId("Player 2");
        opponentDatas[num].SetTeamColor(opponent.team);
        opponent.onDamage.AddListener(opponentDatas[num].fillBar.UpdateFillBar);
    }
}