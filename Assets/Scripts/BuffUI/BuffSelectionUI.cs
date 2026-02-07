using UnityEngine;

public class BuffSelectionUI : MonoBehaviour
{
    public static BuffSelectionUI Instance;

    public GameObject panel;
    public Transform cardContainer;
    public GameObject cardPrefab;

    void Awake()
    {
        Instance = this;
        panel.SetActive(false);
    }

    // [Called by BuffManager RPC]
    public void OpenSelection(int[] buffIds)
    {
        panel.SetActive(true);

        // 기존 카드 제거
        foreach (Transform child in cardContainer)
            Destroy(child.gameObject);

        // 새 카드 생성
        foreach (int id in buffIds)
        {
            BuffData data = BuffDatabase.Instance.GetBuffByID(id);
            if (data == null) continue;

            GameObject card = Instantiate(cardPrefab, cardContainer);
            card.GetComponent<BuffCardUI>().Setup(data, id, this);
        }
    }

    public void OnCardSelected(int buffId)
    {
        panel.SetActive(false);
        // 선택 결과를 BuffManager를 통해 서버로 전송
        BuffManager.Instance.SendSelectionToServer(buffId);
    }
}