using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = System.Random;

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

    //private int[] currentIndices = new int[3];
    private TextMeshProUGUI _timerText;
    private bool _hasSelectedThisPhase = false;

    Player _myPlayer;

    static List<int> GetUniqueRandomNumbers(int min, int max, int count)
    {
        // 1. 범위 내의 모든 숫자 리스트 생성
        List<int> numbers = Enumerable.Range(min, max - min + 1).ToList();
        Random rand = new Random();

        // 2. Fisher-Yates Shuffle 적용
        for (int i = numbers.Count - 1; i > 0; i--)
        {
            int j = rand.Next(i + 1);
            // 값 맞교환 (Swap)
            int temp = numbers[i];
            numbers[i] = numbers[j];
            numbers[j] = temp;
        }

        // 3. 앞에서부터 필요한 개수만큼 추출
        return numbers.Take(count).ToList();
    }

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
        int randomButton = UnityEngine.Random.Range(0, 3);
        OnBuffClicked(randomButton);
    }

    public void OpenCardSelection(int rank)
    {
        cardPanel.SetActive(true);

        cardRank = 3;
        switch (rank)
        {
            case 1: 
                cardRank = 1;
                break;
            case 2:
            case 3:
                cardRank = 2;
                break;
            case 4:
                cardRank = 3;
                break;
        }

        _hasSelectedThisPhase = false; // Reset selection flag for the new phase
        SetupRandomCards();
    }

    void SetupRandomCards()
    {
        if (_myPlayer == null)
            _myPlayer = GetMyPlayer();

        int count = BuffDB.Instance.GetBuffCount(cardRank);

        if (!_myPlayer.hasAbility)
        {
            List<int> randList = GetUniqueRandomNumbers(0, count - 1, 2);

            for (int i = 0; i < 2; i++)
            {
                int buffNum = randList[i];

                // Fetch the actual buff to show user-friendly labels
                Buff b = BuffDB.Instance.GetBuff(cardRank, buffNum);

                if (b != null)
                    cardTexts[i].text = $"{b.buffName}\n<size=24><color=grey>\n{b.discription}<size=20></color></size>";
                else
                {
                    cardTexts[i].text = $"Rank {cardRank}\nBuff {buffNum}";
                    break;
                }

                if (cardButtons[i] != null)
                {
                    cardButtons[i].interactable = true;
                    cardButtons[i].onClick.RemoveAllListeners();
                    cardButtons[i].onClick.AddListener(() => OnBuffClicked(buffNum));
                }
            }

            count = AbilityDB.Instance.GetAbilityCount();
            int abilNum = UnityEngine.Random.Range(1, count + 1);

            Ability a = AbilityDB.Instance.SetAbility(abilNum);
            if (a != null)
                cardTexts[2].text = $"{a.skillName}\n<size=24><color=grey>\n{a.discription}<size=20></color></size>";
            else
            {
                cardTexts[2].text = $"Ability {abilNum}";
                return;
            }

            if (cardButtons[2] != null)
            {
                cardButtons[2].interactable = true;
                cardButtons[2].onClick.RemoveAllListeners();
                cardButtons[2].onClick.AddListener(() => OnAbilityClicked(abilNum));
            }
        }
        else
        {
            List<int> randList = GetUniqueRandomNumbers(0, count - 1, 3);

            for (int i = 0; i < 3; i++)
            {
                int buffNum = randList[i];

                // Fetch the actual buff to show user-friendly labels
                Buff b = BuffDB.Instance.GetBuff(cardRank, buffNum);

                if (b != null)
                    cardTexts[i].text = $"{b.buffName}\n<size=24><color=grey>\n{b.discription}<size=20></color></size>";
                else
                {
                    cardTexts[i].text = $"Rank {cardRank}\nBuff {buffNum}";
                    break;
                }

                if (cardButtons[i] != null)
                {
                    cardButtons[i].interactable = true;
                    cardButtons[i].onClick.RemoveAllListeners();
                    cardButtons[i].onClick.AddListener(() => OnBuffClicked(buffNum));
                }
            }
        }
    }

    public void OnBuffClicked(int buffNumber)
    {
        _hasSelectedThisPhase = true;

        if (_myPlayer != null)
            _myPlayer.RPC_SelectBuff(cardRank, buffNumber);

        // Disable all buttons to prevent clicking again
        for (int i = 0; i < cardButtons.Length; i++)
        {
            if (cardButtons[i] != null)
                cardButtons[i].interactable = false;
        }
    }

    public void OnAbilityClicked(int abilNumber)
    {
        _hasSelectedThisPhase = true;

        if (_myPlayer != null)
            _myPlayer.Rpc_SelectAbility(abilNumber);

        // Disable all buttons to prevent clicking again
        for (int i = 0; i < cardButtons.Length; i++)
        {
            if (cardButtons[i] != null)
                cardButtons[i].interactable = false;
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