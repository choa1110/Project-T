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

    bool _isTeamMatch;

    void Awake()
    {
        if (_instance == null)
            _instance = this;

        gameObject.AddComponent<ScoreDisplayUI>();
    }

    public void SetOpponentUI(Player mainChar)
    {
        List<Player> list = GameManager.Instance.playerList;
        int displayIndex = 0;

        // Check if GameMode is Team Match (1)
        if (GameManager.Instance.Runner.SessionInfo != null &&
            GameManager.Instance.Runner.SessionInfo.Properties.TryGetValue("GameMode", out var gmProp))
        {
            _isTeamMatch = (int)gmProp == 1;
        }

        if (_isTeamMatch)
        {
            displayIndex = 1;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == mainChar)
                    continue;

                if (list[i].team == mainChar.team)
                {
                    LinkOpponent(list[i], 0, true);
                    continue;
                }

                if (displayIndex < opponentDatas.Count)
                {
                    LinkOpponent(list[i], displayIndex);
                    displayIndex++;
                }
            }
        }
        else
        {
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
    }

    void LinkOpponent(Player opponent, int num, bool team = false)
    {
        opponentDatas[num].gameObject.SetActive(true);
        opponent.linkedOpponentData = opponentDatas[num];

        opponentDatas[num].SetOppentState(_isTeamMatch ,team);
        opponentDatas[num].SetOpponentId(opponent.NickName.ToString());
        opponentDatas[num].SetOppentIcon(opponent.charIcon);
        opponentDatas[num].SetTeamColor(opponent.team);

        if (opponentDatas[num].fillBar != null)
        {
            opponentDatas[num].fillBar.SetFillBarInstant(1.0f);
        }
        opponent.onHPChange.AddListener(opponentDatas[num].fillBar.UpdateFillBar);
    }
}