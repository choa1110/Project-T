using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 버프 카드 슬롯 하나.
/// BuffSelectionUI의 cardSlots 리스트에 3개 등록해서 사용.
/// </summary>
public class BuffCardSlot : MonoBehaviour
{
    public Image buffIcon;
    public TMP_Text buffNameText;
    public TMP_Text buffDescText;
    public TMP_Text buffRankText;
    public Button selectButton;

    public void Setup(Buff buff, Action onSelected)
    {
        if (buff == null)
        {
            gameObject.SetActive(false);
            return;
        }

        if (buffIcon != null) { buffIcon.sprite = buff.icon; buffIcon.enabled = buff.icon != null; }
        if (buffNameText != null) buffNameText.text = buff.buffName;
        if (buffDescText != null) buffDescText.text = buff.discription;
        if (buffRankText != null) buffRankText.text = "Rank " + buff.rank;

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => onSelected?.Invoke());
        }
    }
}
