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

    public void SetOpponentUI(Player mainChar)
    {
        List<Player> list = GameManager.Instance.playerList;
        int displayIndex = 0;

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] == mainChar)
                continue;

            if (displayIndex < opponentDatas.Count)
            {
                LinkOpponent(list[i], displayIndex);
                displayIndex++;
            }
        }
    }

    void LinkOpponent(Player opponent, int num)
    {
        opponentDatas[num].gameObject.SetActive(true);
        opponent.linkedOpponentData = opponentDatas[num];
        
        string displayName = opponent.NickName.ToString();

        // Check if GameMode is Team Match (1)
        bool isTeamMatch = false;
        if (GameManager.Instance.Runner.SessionInfo != null && 
            GameManager.Instance.Runner.SessionInfo.Properties.TryGetValue("GameMode", out var gmProp))
        {
            isTeamMatch = (int)gmProp == 1;
        }

        if (isTeamMatch)
        {
            var myPlayer = GameManager.Instance.playerList.Find(p => p.Object.HasInputAuthority);
            if (myPlayer != null && myPlayer.team == opponent.team)
            {
                displayName = $"[TEAM] {displayName}";
            }
            else
            {
                displayName = $"[ENEMY] {displayName}";
            }
        }

        opponentDatas[num].SetOpponentId(displayName);
        opponentDatas[num].SetTeamColor(opponent.team);
        opponent.onHPChange.AddListener(opponentDatas[num].fillBar.UpdateFillBar);
    }
}