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
        for (int i = 0; i < 3; i++)
        {
            currentIndices[i] = Random.Range(0, 3);

            cardTexts[i].text = $"Rank {rank}\nBuff {currentIndices[i]}";

            int buttonNumber = i; 
            cardButtons[i].onClick.RemoveAllListeners();
            cardButtons[i].onClick.AddListener(() => OnCardClicked(buttonNumber));
        }
    }
    public void OnCardClicked(int buttonNumber)
    {
        Player myPlayer = GetMyPlayer();

        if(myPlayer != null)
        {
            myPlayer.RPC_SelectCard(buttonNumber);
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
