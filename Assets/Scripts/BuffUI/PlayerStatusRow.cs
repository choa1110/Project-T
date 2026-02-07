using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStatusRow : MonoBehaviour
{
    public Text nameText;           // 플레이어 이름 (예: Player 1)
    public Transform iconContainer; // 버프 아이콘들이 나열될 부모 오브젝트
    public GameObject iconPrefab;   // 버프 아이콘 프리팹 (이미지 하나 달랑 있는 것)

    public void SetInfo(string playerName, List<BuffData> buffs)
    {
        nameText.text = playerName;

        // 기존 아이콘 싹 지우고 새로 그리기 (Refresh)
        foreach (Transform child in iconContainer)
            Destroy(child.gameObject);

        foreach (var buff in buffs)
        {
            if (buff.icon == null) continue; // 아이콘 없으면 스킵

            GameObject iconObj = Instantiate(iconPrefab, iconContainer);
            iconObj.GetComponent<Image>().sprite = buff.icon;

            // (선택) 마우스 올리면 툴팁 뜨게 하려면 여기에 컴포넌트 추가
        }
    }
}