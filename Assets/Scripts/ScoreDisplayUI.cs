using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using UnityEngine.SceneManagement;

public class ScoreDisplayUI : MonoBehaviour
{
    public static ScoreDisplayUI Instance { get; private set; }

    private GameObject interimPanel;
    private TextMeshProUGUI interimTitle;
    private Transform interimListParent;

    private GameObject finalPanel;
    private TextMeshProUGUI finalTitle;
    private Transform finalListParent;
    private Button playAgainButton;
    private Button leaveButton;
    private TextMeshProUGUI playAgainText;

    private TMP_FontAsset themeFont;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Try to grab the theme font from HUDManager to match UI styling
        if (HUDManager.Instance != null && HUDManager.Instance.charName != null)
        {
            themeFont = HUDManager.Instance.charName.font;
        }

        CreateUI();
    }

    private GameObject CreatePanel(string name, Transform parent, Color bgColor)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;

        Image img = go.GetComponent<Image>();
        img.color = bgColor;
        img.raycastTarget = true; // Block raycasts to objects behind the UI

        return go;
    }

    private TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        if (themeFont != null)
        {
            tmp.font = themeFont;
        }
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        tmp.raycastTarget = false;

        return tmp;
    }

    private Button CreateButton(string name, Transform parent, string label, Color normalColor, System.Action onClickAction, out TextMeshProUGUI labelTextOut)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(220, 55);

        Image img = go.GetComponent<Image>();
        img.color = normalColor;
        img.raycastTarget = true;

        Button btn = go.GetComponent<Button>();
        btn.targetGraphic = img;

        // Visual states via ColorBlock
        ColorBlock colors = btn.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = new Color(Mathf.Clamp01(normalColor.r + 0.15f), Mathf.Clamp01(normalColor.g + 0.15f), Mathf.Clamp01(normalColor.b + 0.15f), 1f);
        colors.pressedColor = new Color(Mathf.Clamp01(normalColor.r - 0.15f), Mathf.Clamp01(normalColor.g - 0.15f), Mathf.Clamp01(normalColor.b - 0.15f), 1f);
        colors.selectedColor = normalColor;
        colors.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.1f;
        btn.colors = colors;

        btn.onClick.AddListener(() => onClickAction?.Invoke());

        // Text label
        labelTextOut = CreateText(name + "_Label", go.transform, label, 20, Color.white);
        RectTransform textRt = labelTextOut.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;

        return btn;
    }

    private void CreateUI()
    {
        // ------------------ 1. Interim Panel Setup ------------------
        interimPanel = CreatePanel("InterimScorePanel", transform, new Color(0.05f, 0.06f, 0.09f, 0.95f));
        interimPanel.SetActive(false);

        GameObject interimContent = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
        interimContent.transform.SetParent(interimPanel.transform, false);
        RectTransform interimContentRt = interimContent.GetComponent<RectTransform>();
        interimContentRt.anchorMin = new Vector2(0.2f, 0.1f);
        interimContentRt.anchorMax = new Vector2(0.8f, 0.9f);
        interimContentRt.sizeDelta = Vector2.zero;

        VerticalLayoutGroup interimVlg = interimContent.GetComponent<VerticalLayoutGroup>();
        interimVlg.spacing = 15;
        interimVlg.childAlignment = TextAnchor.UpperCenter;
        interimVlg.childControlHeight = false;
        interimVlg.childControlWidth = true;
        interimVlg.childForceExpandHeight = false;
        interimVlg.childForceExpandWidth = true;

        interimTitle = CreateText("Title", interimContent.transform, "ROUND 1 RESULTS", 38, new Color(0.3f, 0.75f, 1f, 1f));
        CreateText("Subtitle", interimContent.transform, "Round Eliminations", 18, Color.gray);

        GameObject interimList = new GameObject("ListParent", typeof(RectTransform), typeof(VerticalLayoutGroup));
        interimList.transform.SetParent(interimContent.transform, false);
        interimListParent = interimList.transform;
        
        VerticalLayoutGroup interimListVlg = interimList.GetComponent<VerticalLayoutGroup>();
        interimListVlg.spacing = 8;
        interimListVlg.childAlignment = TextAnchor.UpperCenter;
        interimListVlg.childControlHeight = false;
        interimListVlg.childControlWidth = true;
        interimListVlg.childForceExpandHeight = false;
        interimListVlg.childForceExpandWidth = true;

        // ------------------ 2. Final Panel Setup ------------------
        finalPanel = CreatePanel("FinalRankingsPanel", transform, new Color(0.03f, 0.03f, 0.05f, 0.97f));
        finalPanel.SetActive(false);

        GameObject finalContent = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
        finalContent.transform.SetParent(finalPanel.transform, false);
        RectTransform finalContentRt = finalContent.GetComponent<RectTransform>();
        finalContentRt.anchorMin = new Vector2(0.15f, 0.08f);
        finalContentRt.anchorMax = new Vector2(0.85f, 0.92f);
        finalContentRt.sizeDelta = Vector2.zero;

        VerticalLayoutGroup finalVlg = finalContent.GetComponent<VerticalLayoutGroup>();
        finalVlg.spacing = 15;
        finalVlg.childAlignment = TextAnchor.UpperCenter;
        finalVlg.childControlHeight = false;
        finalVlg.childControlWidth = true;
        finalVlg.childForceExpandHeight = false;
        finalVlg.childForceExpandWidth = true;

        finalTitle = CreateText("Title", finalContent.transform, "FINAL RANKINGS", 46, new Color(1f, 0.78f, 0.15f, 1f));
        CreateText("Subtitle", finalContent.transform, "Tally of all rounds", 18, Color.gray);

        GameObject finalList = new GameObject("ListParent", typeof(RectTransform), typeof(VerticalLayoutGroup));
        finalList.transform.SetParent(finalContent.transform, false);
        finalListParent = finalList.transform;

        VerticalLayoutGroup finalListVlg = finalList.GetComponent<VerticalLayoutGroup>();
        finalListVlg.spacing = 8;
        finalListVlg.childAlignment = TextAnchor.UpperCenter;
        finalListVlg.childControlHeight = false;
        finalListVlg.childControlWidth = true;
        finalListVlg.childForceExpandHeight = false;
        finalListVlg.childForceExpandWidth = true;

        // Button Container
        GameObject btnPanel = new GameObject("ButtonPanel", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        btnPanel.transform.SetParent(finalContent.transform, false);
        RectTransform btnPanelRt = btnPanel.GetComponent<RectTransform>();
        btnPanelRt.sizeDelta = new Vector2(500, 60);

        HorizontalLayoutGroup btnHlg = btnPanel.GetComponent<HorizontalLayoutGroup>();
        btnHlg.spacing = 30;
        btnHlg.childAlignment = TextAnchor.MiddleCenter;
        btnHlg.childControlHeight = false;
        btnHlg.childControlWidth = false;

        leaveButton = CreateButton("LeaveButton", btnPanel.transform, "LEAVE ROOM", new Color(0.24f, 0.27f, 0.31f, 1f), OnLeaveClicked, out _);
        playAgainButton = CreateButton("PlayAgainButton", btnPanel.transform, "PLAY AGAIN", new Color(0.35f, 0.28f, 0.78f, 1f), OnPlayAgainClicked, out playAgainText);
    }

    private void CreateRow(Transform parent, int rank, string playerName, string midText, string rightText, bool isLocalPlayer)
    {
        GameObject row = new GameObject("PlayerRow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(parent, false);

        RectTransform rt = row.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 48);

        Image img = row.GetComponent<Image>();
        img.color = isLocalPlayer ? new Color(0.18f, 0.26f, 0.45f, 0.9f) : new Color(0.1f, 0.11f, 0.15f, 0.85f);

        HorizontalLayoutGroup hlg = row.GetComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(25, 25, 0, 0);
        hlg.spacing = 15;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlHeight = true;
        hlg.childControlWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childForceExpandWidth = false;

        // Rank Text
        string rankStr = rank > 0 ? $"{rank}." : "";
        var rankTxt = CreateText("RankText", row.transform, rankStr, 20, isLocalPlayer ? new Color(1f, 0.8f, 0.2f, 1f) : Color.white, TextAlignmentOptions.Left);
        rankTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(60, 0);

        // Name Text
        var nameTxt = CreateText("NameText", row.transform, playerName, 20, Color.white, TextAlignmentOptions.Left);
        nameTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(250, 0);

        // Mid Text
        var midTxt = CreateText("MidText", row.transform, midText, 16, new Color(0.7f, 0.7f, 0.7f, 1f), TextAlignmentOptions.Center);
        midTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(250, 0);

        // Right Text
        var rightTxt = CreateText("RightText", row.transform, rightText, 20, isLocalPlayer ? new Color(1f, 0.8f, 0.2f, 1f) : Color.white, TextAlignmentOptions.Right);
        rightTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 0);
    }

    private void CreateTeamHeader(Transform parent, string teamName, int totalKills, Color color)
    {
        GameObject row = new GameObject("TeamHeader", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(parent, false);

        RectTransform rt = row.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 44);

        Image img = row.GetComponent<Image>();
        img.color = new Color(color.r, color.g, color.b, 0.2f);

        HorizontalLayoutGroup hlg = row.GetComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(25, 25, 0, 0);
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlHeight = true;
        hlg.childControlWidth = true;
        hlg.childForceExpandHeight = true;
        hlg.childForceExpandWidth = true;

        var nameTxt = CreateText("TeamNameText", row.transform, teamName, 22, color, TextAlignmentOptions.Left);
        var scoreTxt = CreateText("TeamKillsText", row.transform, $"Total: {totalKills} Kills ({totalKills * 3} pts)", 20, Color.white, TextAlignmentOptions.Right);
    }

    private void CreateTeamMemberRow(Transform parent, Player player, int roundNum)
    {
        int kills = player.RoundEliminations.Get(roundNum - 1);
        int score = kills * 3;
        CreateRow(parent, 0, $"   • {player.NickName}", $"{kills} Kills", $"+{score} pts", player.Object.HasInputAuthority);
    }

    private void CreateFinalTeamMemberRow(Transform parent, Player player)
    {
        int totalKills = 0;
        string detailText = "";
        for (int r = 0; r < GameManager.Instance.MaxRounds; r++)
        {
            int rKills = player.RoundEliminations.Get(r);
            totalKills += rKills;
            if (r > 0) detailText += " | ";
            detailText += $"R{r + 1}: {rKills}";
        }
        int totalScore = totalKills * 3;

        CreateRow(parent, 0, $"   • {player.NickName}", detailText, $"{totalKills} Kills ({totalScore} pts)", player.Object.HasInputAuthority);
    }

    public void ShowInterimScores(int roundNum)
    {
        interimPanel.SetActive(true);
        interimTitle.text = $"ROUND {roundNum} RESULTS";

        foreach (Transform child in interimListParent)
        {
            Destroy(child.gameObject);
        }

        var players = GameManager.Instance.playerList.Where(p => p != null).ToList();

        // Check if GameMode is Team Match
        bool isTeamMatch = false;
        var runner = GameManager.Instance.Runner;
        if (runner != null && runner.SessionInfo != null && 
            runner.SessionInfo.Properties.TryGetValue("GameMode", out var gmProp))
        {
            isTeamMatch = (int)gmProp == 1;
        }

        if (isTeamMatch)
        {
            // Group by team: 0 = RED, 1 = BLUE
            var redPlayers = players.Where(p => p.team == 0).ToList();
            var bluePlayers = players.Where(p => p.team == 1).ToList();

            int redKills = redPlayers.Sum(p => p.RoundEliminations.Get(roundNum - 1));
            int blueKills = bluePlayers.Sum(p => p.RoundEliminations.Get(roundNum - 1));

            bool redFirst = redKills >= blueKills;

            if (redFirst)
            {
                CreateTeamHeader(interimListParent, "RED TEAM", redKills, new Color(1f, 0.35f, 0.35f, 1f));
                foreach (var p in redPlayers.OrderByDescending(p => p.RoundEliminations.Get(roundNum - 1)))
                {
                    CreateTeamMemberRow(interimListParent, p, roundNum);
                }

                CreateTeamHeader(interimListParent, "BLUE TEAM", blueKills, new Color(0.35f, 0.55f, 1f, 1f));
                foreach (var p in bluePlayers.OrderByDescending(p => p.RoundEliminations.Get(roundNum - 1)))
                {
                    CreateTeamMemberRow(interimListParent, p, roundNum);
                }
            }
            else
            {
                CreateTeamHeader(interimListParent, "BLUE TEAM", blueKills, new Color(0.35f, 0.55f, 1f, 1f));
                foreach (var p in bluePlayers.OrderByDescending(p => p.RoundEliminations.Get(roundNum - 1)))
                {
                    CreateTeamMemberRow(interimListParent, p, roundNum);
                }

                CreateTeamHeader(interimListParent, "RED TEAM", redKills, new Color(1f, 0.35f, 0.35f, 1f));
                foreach (var p in redPlayers.OrderByDescending(p => p.RoundEliminations.Get(roundNum - 1)))
                {
                    CreateTeamMemberRow(interimListParent, p, roundNum);
                }
            }
        }
        else
        {
            var sortedPlayers = players
                .OrderByDescending(p => p.RoundEliminations.Get(roundNum - 1))
                .ToList();

            for (int i = 0; i < sortedPlayers.Count; i++)
            {
                var player = sortedPlayers[i];
                int kills = player.RoundEliminations.Get(roundNum - 1);
                int score = kills * 3;
                CreateRow(interimListParent, i + 1, player.NickName.ToString(), $"{kills} Kills", $"+{score} pts", player.Object.HasInputAuthority);
            }
        }

        StartCoroutine(AnimateScale(interimPanel.transform));
        StartCoroutine(InterimTransitionCoroutine(roundNum));
    }

    private IEnumerator InterimTransitionCoroutine(int roundNum)
    {
        yield return new WaitForSeconds(5f);
        
        interimPanel.SetActive(false);
    }

    public void ShowFinalRankings()
    {
        finalPanel.SetActive(true);

        foreach (Transform child in finalListParent)
        {
            Destroy(child.gameObject);
        }

        var players = GameManager.Instance.playerList.Where(p => p != null).ToList();

        // Check if GameMode is Team Match
        bool isTeamMatch = false;
        var runner = GameManager.Instance.Runner;
        if (runner != null && runner.SessionInfo != null && 
            runner.SessionInfo.Properties.TryGetValue("GameMode", out var gmProp))
        {
            isTeamMatch = (int)gmProp == 1;
        }

        // Determine Win/Loss/Draw title and color for local player
        string titleText = "FINAL RANKINGS";
        Color titleColor = new Color(1f, 0.78f, 0.15f, 1f); // default gold

        Player localPlayer = players.Find(p => p != null && p.Object != null && p.Object.HasInputAuthority);

        if (localPlayer != null)
        {
            if (isTeamMatch)
            {
                var redPlayers = players.Where(p => p.team == 0).ToList();
                var bluePlayers = players.Where(p => p.team == 1).ToList();

                int redTotalKills = 0;
                int blueTotalKills = 0;

                for (int r = 0; r < GameManager.Instance.MaxRounds; r++)
                {
                    redTotalKills += redPlayers.Sum(p => p.RoundEliminations.Get(r));
                    blueTotalKills += bluePlayers.Sum(p => p.RoundEliminations.Get(r));
                }

                if (redTotalKills == blueTotalKills)
                {
                    titleText = "DRAW";
                    titleColor = new Color(0.6f, 0.6f, 0.6f, 1f); // Gray
                }
                else
                {
                    bool redWins = redTotalKills > blueTotalKills;
                    bool isLocalRed = localPlayer.team == 0;

                    if ((redWins && isLocalRed) || (!redWins && !isLocalRed))
                    {
                        titleText = "🏆 VICTORY 🏆";
                        titleColor = new Color(0.3f, 0.9f, 0.3f, 1f); // Green
                    }
                    else
                    {
                        titleText = "DEFEAT";
                        titleColor = new Color(0.9f, 0.2f, 0.2f, 1f); // Red
                    }
                }
            }
            else
            {
                // Solo Mode
                var playerTotals = players.Select(p => {
                    int totalKills = 0;
                    for (int r = 0; r < GameManager.Instance.MaxRounds; r++)
                    {
                        totalKills += p.RoundEliminations.Get(r);
                    }
                    int totalScore = totalKills * 3;
                    return new { Player = p, TotalKills = totalKills, TotalScore = totalScore };
                })
                .OrderByDescending(pt => pt.TotalScore)
                .ToList();

                if (playerTotals.Count > 0)
                {
                    int maxScore = playerTotals[0].TotalScore;
                    var topScorers = playerTotals.Where(pt => pt.TotalScore == maxScore).ToList();

                    var localTotal = playerTotals.Find(pt => pt.Player == localPlayer);
                    if (localTotal != null)
                    {
                        if (localTotal.TotalScore == maxScore)
                        {
                            if (topScorers.Count == 1)
                            {
                                titleText = "🏆 VICTORY 🏆";
                                titleColor = new Color(0.3f, 0.9f, 0.3f, 1f); // Green
                            }
                            else
                            {
                                titleText = "DRAW";
                                titleColor = new Color(0.6f, 0.6f, 0.6f, 1f); // Gray
                            }
                        }
                        else
                        {
                            titleText = "DEFEAT";
                            titleColor = new Color(0.9f, 0.2f, 0.2f, 1f); // Red
                        }
                    }
                }
            }
        }

        finalTitle.text = titleText;
        finalTitle.color = titleColor;

        if (isTeamMatch)
        {
            var redPlayers = players.Where(p => p.team == 0).ToList();
            var bluePlayers = players.Where(p => p.team == 1).ToList();

            int redTotalKills = 0;
            int blueTotalKills = 0;

            for (int r = 0; r < GameManager.Instance.MaxRounds; r++)
            {
                redTotalKills += redPlayers.Sum(p => p.RoundEliminations.Get(r));
                blueTotalKills += bluePlayers.Sum(p => p.RoundEliminations.Get(r));
            }

            bool redWins = redTotalKills > blueTotalKills;
            bool blueWins = blueTotalKills > redTotalKills;

            if (redWins)
            {
                CreateTeamHeader(finalListParent, "🏆 1st PLACE: RED TEAM", redTotalKills, new Color(1f, 0.35f, 0.35f, 1f));
                foreach (var p in redPlayers)
                {
                    CreateFinalTeamMemberRow(finalListParent, p);
                }

                CreateTeamHeader(finalListParent, "2nd PLACE: BLUE TEAM", blueTotalKills, new Color(0.35f, 0.55f, 1f, 1f));
                foreach (var p in bluePlayers)
                {
                    CreateFinalTeamMemberRow(finalListParent, p);
                }
            }
            else if (blueWins)
            {
                CreateTeamHeader(finalListParent, "🏆 1st PLACE: BLUE TEAM", blueTotalKills, new Color(0.35f, 0.55f, 1f, 1f));
                foreach (var p in bluePlayers)
                {
                    CreateFinalTeamMemberRow(finalListParent, p);
                }

                CreateTeamHeader(finalListParent, "2nd PLACE: RED TEAM", redTotalKills, new Color(1f, 0.35f, 0.35f, 1f));
                foreach (var p in redPlayers)
                {
                    CreateFinalTeamMemberRow(finalListParent, p);
                }
            }
            else // Tied / Draw
            {
                CreateTeamHeader(finalListParent, "🤝 DRAW: RED TEAM", redTotalKills, new Color(1f, 0.35f, 0.35f, 1f));
                foreach (var p in redPlayers)
                {
                    CreateFinalTeamMemberRow(finalListParent, p);
                }

                CreateTeamHeader(finalListParent, "🤝 DRAW: BLUE TEAM", blueTotalKills, new Color(0.35f, 0.55f, 1f, 1f));
                foreach (var p in bluePlayers)
                {
                    CreateFinalTeamMemberRow(finalListParent, p);
                }
            }
        }
        else
        {
            var playerTotals = players.Select(p => {
                int totalKills = 0;
                for (int r = 0; r < GameManager.Instance.MaxRounds; r++)
                {
                    totalKills += p.RoundEliminations.Get(r);
                }
                int totalScore = totalKills * 3;
                return new { Player = p, TotalKills = totalKills, TotalScore = totalScore };
            })
            .OrderByDescending(pt => pt.TotalScore)
            .ToList();

            for (int i = 0; i < playerTotals.Count; i++)
            {
                var pt = playerTotals[i];
                string detailText = "";
                for (int r = 0; r < GameManager.Instance.MaxRounds; r++)
                {
                    if (r > 0) detailText += " | ";
                    detailText += $"R{r + 1}: {pt.Player.RoundEliminations.Get(r)}";
                }

                CreateRow(finalListParent, i + 1, pt.Player.NickName.ToString(), detailText, $"{pt.TotalKills} Kills ({pt.TotalScore} pts)", pt.Player.Object.HasInputAuthority);
            }
        }

        var runnerObj = GameManager.Instance.Runner;
        if (runnerObj != null)
        {
            if (runnerObj.IsServer)
            {
                playAgainButton.interactable = true;
                playAgainText.text = "PLAY AGAIN";
            }
            else
            {
                playAgainButton.interactable = false;
                playAgainText.text = "WAITING FOR HOST...";
            }
        }

        StartCoroutine(AnimateScale(finalPanel.transform));
    }

    private IEnumerator AnimateScale(Transform target)
    {
        target.localScale = new Vector3(0.85f, 0.85f, 1f);
        float time = 0f;
        while (time < 0.2f)
        {
            time += Time.unscaledDeltaTime;
            float t = time / 0.2f;
            target.localScale = Vector3.Lerp(new Vector3(0.85f, 0.85f, 1f), Vector3.one, Mathf.Sin(t * Mathf.PI * 0.5f));
            yield return null;
        }
        target.localScale = Vector3.one;
    }

    private void OnLeaveClicked()
    {
        var runner = GameManager.Instance.Runner;
        if (runner != null)
        {
            runner.Shutdown();
        }

        SceneManager.LoadScene(0);
    }

    private void OnPlayAgainClicked()
    {
        var runner = GameManager.Instance.Runner;
        if (runner != null && runner.IsServer)
        {
            runner.LoadScene(SceneRef.FromIndex(1));
        }
    }

    public void HideAll()
    {
        if (interimPanel != null) interimPanel.SetActive(false);
        if (finalPanel != null) finalPanel.SetActive(false);
    }
}