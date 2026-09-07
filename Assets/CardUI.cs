using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class CardUI : MonoBehaviour
{
    public GameObject cardPanel;

    public Button cardButton1;
    public Button cardButton2;
    public Button cardButton3;

    public int cardRank;
    public int buffIndex;

    [Header("카드 버튼들")]
    public Button[] cardButtons;
    public TextMeshProUGUI[] cardTexts;

    private int[] currentIndices = new int[3];
    private TextMeshProUGUI _timerText;
    private bool _hasSelectedThisPhase = false;

    Player _myPlayer;

    void Awake()
    {
        // Dynamically create a timer text above the selection area if not already present
        if (cardPanel != null)
        {
            GameObject timerGo = new GameObject("CardSelectionTimerText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            timerGo.transform.SetParent(cardPanel.transform, false);

            _timerText = timerGo.GetComponent<TextMeshProUGUI>();
            _timerText.fontSize = 28;
            _timerText.color = new Color(1f, 0.35f, 0.35f, 1f); // Vibrant orange-red
            _timerText.alignment = TextAlignmentOptions.Center;
            _timerText.fontStyle = FontStyles.Bold;

            // Positioning it above the buttons
            RectTransform rt = timerGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.85f);
            rt.anchorMax = new Vector2(0.5f, 0.85f);
            rt.sizeDelta = new Vector2(400, 50);
            rt.anchoredPosition = new Vector2(0, 0);

            if (cardTexts != null && cardTexts.Length > 0 && cardTexts[0] != null)
            {
                _timerText.font = cardTexts[0].font;
            }
        }
    }

    void Start()
    {
        cardPanel.SetActive(false);
    }

    void Update()
    {
        if (cardPanel != null && cardPanel.activeSelf)
        {
            if (GameManager.Instance != null)
            {
                bool allSelected = GameManager.Instance.playerList.Count > 0 && 
                                   GameManager.Instance.playerList.All(p => p.HasSelectedCard);

                if (allSelected)
                {
                    if (_timerText != null)
                    {
                        _timerText.text = "All players have selected; the round is starting.";
                    }
                }
                else
                {
                    float remainingTime = GameManager.Instance.GetCardSelectionRemainingTime();
                    
                    if (_timerText != null)
                    {
                        _timerText.text = $"TIME REMAINING: {remainingTime:F1}s";
                    }

                    // If timer expired and panel is still open (and we haven't selected yet), auto select a random card
                    if (remainingTime <= 0f && !_hasSelectedThisPhase)
                    {
                        SelectRandomCard();
                    }
                }
            }
        }
    }

    void SelectRandomCard()
    {
        _hasSelectedThisPhase = true;
        int randomButton = Random.Range(0, 3);
        OnBuffClicked(randomButton);
    }

    public void OpenCardSelection(int rank)
    {
        cardPanel.SetActive(true);
        cardRank = rank;
        _hasSelectedThisPhase = false; // Reset selection flag for the new phase
        SetupRandomCards(rank);
    }

    void SetupRandomCards(int rank)
    {
        if (_myPlayer == null)
            _myPlayer = GetMyPlayer();

        int count = BuffDB.Instance.GetBuffCount(rank);
        if (count == 0)
        {
            count = BuffDB.Instance.GetBuffCount(1); // Fallback to Rank 1 count
        }
        if (count == 0) count = 1; // Absolute fallback

        for (int i = 0; i < 2; i++)
        {
            // Pick a random buff index (0 to count-1)
            currentIndices[i] = Random.Range(0, count);

            // Fetch the actual buff to show user-friendly labels
            Buff b = BuffDB.Instance.GetBuff(rank, currentIndices[i] + 1);
            if (b != null)
            {
                cardTexts[i].text = $"{b.buffName}\n<size=14><color=grey>{b.discription}</color></size>";
            }
            else
            {
                cardTexts[i].text = $"Rank {rank}\nBuff {currentIndices[i]}";
            }

            int buttonNumber = i; 
            if (cardButtons[i] != null)
            {
                cardButtons[i].interactable = true;
                cardButtons[i].onClick.RemoveAllListeners();
                cardButtons[i].onClick.AddListener(() => OnBuffClicked(currentIndices[i]));
            }
        }

        if (_myPlayer.hasAbility)
        {
            // Pick a random buff index (0 to count-1)
            currentIndices[2] = Random.Range(0, count);

            // Fetch the actual buff to show user-friendly labels
            Buff b = BuffDB.Instance.GetBuff(rank, currentIndices[2] + 1);
            if (b != null)
            {
                cardTexts[2].text = $"{b.buffName}\n<size=14><color=grey>{b.discription}</color></size>";
            }
            else
            {
                cardTexts[2].text = $"Rank {rank}\nBuff {currentIndices[2]}";
            }

            if (cardButtons[2] != null)
            {
                cardButtons[2].interactable = true;
                cardButtons[2].onClick.RemoveAllListeners();
                cardButtons[2].onClick.AddListener(() => OnBuffClicked(currentIndices[2]));
            }
        }
        else
        {
            count = AbilityDB.Instance.GetAbilityCount();
            currentIndices[2] = Random.Range(1, count + 1);

            Ability a = AbilityDB.Instance.SetAbility(currentIndices[2]);
            if (a != null)
            {
                cardTexts[2].text = $"{a.skillName}\n<size=14><color=grey>{a.discription}</color></size>";
            }
            else
            {
                cardTexts[2].text = $"Rank {rank}\nBuff {currentIndices[2]}";
            }

            if (cardButtons[2] != null)
            {
                cardButtons[2].interactable = true;
                cardButtons[2].onClick.RemoveAllListeners();
                cardButtons[2].onClick.AddListener(() => OnAbilityClicked(a.skillNum));
            }
        }
    }

    public void OnBuffClicked(int buttonNumber)
    {
        _hasSelectedThisPhase = true;
        int selectedIndex = currentIndices[buttonNumber];

        if (_myPlayer != null)
        {
            _myPlayer.RPC_SelectCard(cardRank, selectedIndex + 1);
        }

        // Disable all buttons to prevent clicking again
        for (int i = 0; i < cardButtons.Length; i++)
        {
            if (cardButtons[i] != null)
            {
                cardButtons[i].interactable = false;
            }
        }
    }

    public void OnAbilityClicked(int abilNumber)
    {
        _hasSelectedThisPhase = true;

        if (_myPlayer != null)
        {
            _myPlayer.Rpc_RequestSetAbility(abilNumber);
        }

        // Disable all buttons to prevent clicking again
        for (int i = 0; i < cardButtons.Length; i++)
        {
            if (cardButtons[i] != null)
            {
                cardButtons[i].interactable = false;
            }
        }
    }

    Player GetMyPlayer()
    {
        foreach(var p in FindObjectsByType<Player>(FindObjectsSortMode.None))
        {
            if (p.Object.HasInputAuthority) return p;
        }

        return null;
    }
}
