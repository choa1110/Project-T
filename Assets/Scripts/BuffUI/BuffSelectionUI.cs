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

        // ���� ī�� ����
        foreach (Transform child in cardContainer)
            Destroy(child.gameObject);

        // �� ī�� ����
        foreach (int id in buffIds)
        {
            Buff data = BuffDatabase.Instance.GetBuffByID(id);
            if (data == null) continue;

            GameObject card = Instantiate(cardPrefab, cardContainer);
            card.GetComponent<BuffCardUI>().Setup(data, id, this);
        }
    }

    public void OnCardSelected(int buffId)
    {
        panel.SetActive(false);
        // ���� ����� BuffManager�� ���� ������ ����
        BuffManager.Instance.SendSelectionToServer(buffId);
    }
}