using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OpponentData : MonoBehaviour
{
    Image _background;

    [SerializeField] Image _opponentIcon;
    [SerializeField] TMP_Text _opponentId;
    public FillBar fillBar;

    bool _teamGame;
    bool _isTeam;

    void Awake()
    {
        _background = GetComponent<Image>();
    }

    public void SetOppentIcon(Sprite icon)
    {
        _opponentIcon.sprite = icon;
    }

    public void SetOppentState(bool isTeamGame, bool isFriendly)
    {
        _teamGame = isTeamGame;
        _isTeam = isFriendly;
    }

    public void SetOpponentId(string name)
    {
        if (_teamGame)
        {
            if (_isTeam)
                name = "[TEAM] " + name;
            else
                name = "[ENEMY] " + name;
        }

        _opponentId.text = name;
    }

    public void SetTeamColor(int teamNum)
    {
        Color newColor;

        switch (teamNum)
        {
            case 0:
                if (ColorUtility.TryParseHtmlString("#000ACC", out newColor))
                    _background.color = newColor;
                break;
            case 1:
                if (ColorUtility.TryParseHtmlString("#DE0000", out newColor))
                    _background.color = newColor;
                break;
            case 2:
                if (ColorUtility.TryParseHtmlString("#00D900", out newColor))
                    _background.color = newColor;
                break;
            case 3:
                if (ColorUtility.TryParseHtmlString("#FFEB04", out newColor))
                    _background.color = newColor;
                break;
            default:
                if (ColorUtility.TryParseHtmlString("#FFFFFF", out newColor))
                    _background.color = newColor;
                break;
        }
    }
}