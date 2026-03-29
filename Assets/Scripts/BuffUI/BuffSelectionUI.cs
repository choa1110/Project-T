using UnityEngine;
using System.Collections.Generic;

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

    public void OpenSelection(int[] buffIds, int round)
    {
        panel.SetActive(true);

        foreach (Transform child in cardContainer)
            Destroy(child.gameObject);

        // 해당 라운드의 전체 버프 리스트를 가져옵니다.
        List<Buff> targetList = BuffDB.Instance.GetBuffListByRank(round);

        foreach (int id in buffIds)
        {
            // 인덱스 아웃 오브 레인지 방지
            if (id >= 0 && id < targetList.Count)
            {
                Buff data = targetList[id];
                if (data == null) continue;

                GameObject card = Instantiate(cardPrefab, cardContainer);
                card.GetComponent<BuffCardUI>().Setup(data, id, this);
            }
        }
    }

    public void OnCardSelected(int buffId)
    {
        panel.SetActive(false);
        BuffManager.Instance.SendSelectionToServer(buffId);
    }
}