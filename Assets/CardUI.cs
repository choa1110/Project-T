using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

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

    void Start()
    {
        cardPanel.SetActive(false);
    }


    public void OpenCardSelection(int rank)
    {
        cardPanel.SetActive(true);
        SetupRandomCards(rank);
    }
    void SetupRandomCards(int rank)
    {
        int count = BuffDB.Instance.GetBuffCount(rank);
        if (count == 0)
        {
            count = BuffDB.Instance.GetBuffCount(1); // Fallback to Rank 1 count
        }
        if (count == 0) count = 1; // Absolute fallback

        for (int i = 0; i < 3; i++)
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
            cardButtons[i].onClick.RemoveAllListeners();
            cardButtons[i].onClick.AddListener(() => OnCardClicked(buttonNumber));
        }
    }
    public void OnCardClicked(int buttonNumber)
    {
        int selectedIndex = currentIndices[buttonNumber];
        Player myPlayer = GetMyPlayer();

        if(myPlayer != null)
        {
            myPlayer.RPC_SelectCard(selectedIndex + 1);
        }

        cardPanel.SetActive(false);
    }

    Player GetMyPlayer()
    {
        foreach(var p in FindObjectsByType<Player>(FindObjectsSortMode.None))
        {
            if(p.Object.HasInputAuthority) return p;
        }
        return null;
    }
}
