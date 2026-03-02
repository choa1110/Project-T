using UnityEngine;
using Fusion; // Fusion ���ӽ����̽� �ʿ�

public class StatusBoardUI : MonoBehaviour
{
    [Header("UI Components")]
    public GameObject boardPanel;       // ���� �״� �� ��ü �г�
    public Transform listContainer;     // Row���� �� Vertical Layout �׷�
    public GameObject rowPrefab;        // ������ ���� PlayerStatusRow ������

    void Start()
    {
        boardPanel.SetActive(false); // ������ �� ���α�
    }

    void Update()
    {
        // �� Ű�� ��� (InputSystem�� �������� UI�� Legacy Input�� ���� ���� �����ϴ�)
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            bool show = !boardPanel.activeSelf;
            boardPanel.SetActive(show);

            if (show) RefreshBoard();
        }
    }

    void RefreshBoard()
    {
        // 1. ���� ��� �ʱ�ȭ
        foreach (Transform child in listContainer)
            Destroy(child.gameObject);

        // 2. ���� �ִ� ��� �÷��̾�(BuffSystem) ã��
        // (Fusion�� ��� FindObjectsOfType���� ã�Ƶ� �ǰ�, Runner.ActivePlayers�� �ᵵ ��)
        var allPlayers = FindObjectsOfType<BuffSystem>();

        foreach (var buffSys in allPlayers)
        {
            // �÷��̾� ��ü�� ���� �ʱ�ȭ �� ������ �н�
            // if (buffSys.Object == null) continue;

            // Row ����
            GameObject rowObj = Instantiate(rowPrefab, listContainer);
            PlayerStatusRow row = rowObj.GetComponent<PlayerStatusRow>();

            // �̸� ���� (Player ID �Ǵ� �г���)
            // string pName = $"Player {buffSys.Object.InputAuthority.PlayerId}";

            // �� �ڽ�(Local Player)���� ǥ�����ָ� ����
            // if (buffSys.Object.HasInputAuthority)
            //     pName += " (Me)";

            // // 3. Phase 2���� ���� GetSyncBuffs()�� ���� ��� ��������!
            // row.SetInfo(pName, buffSys.GetSyncBuffs());
        }
    }
}