using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStatusRow : MonoBehaviour
{
    public Text nameText;           // �÷��̾� �̸� (��: Player 1)
    public Transform iconContainer; // ���� �����ܵ��� ������ �θ� ������Ʈ
    public GameObject iconPrefab;   // ���� ������ ������ (�̹��� �ϳ� �޶� �ִ� ��)

    public void SetInfo(string playerName, List<Buff> buffs)
    {
        nameText.text = playerName;

        // ���� ������ �� ����� ���� �׸��� (Refresh)
        foreach (Transform child in iconContainer)
            Destroy(child.gameObject);

        foreach (var buff in buffs)
        {
            // if (buff.icon == null) continue; // ������ ������ ��ŵ

            GameObject iconObj = Instantiate(iconPrefab, iconContainer);
            // iconObj.GetComponent<Image>().sprite = buff.icon;

            // (����) ���콺 �ø��� ���� �߰� �Ϸ��� ���⿡ ������Ʈ �߰�
        }
    }
}