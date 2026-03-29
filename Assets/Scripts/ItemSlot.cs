using UnityEngine;
using UnityEngine.UI;

public class ItemSlot : MonoBehaviour
{
    public Image slotIcon;

    public void InputSlot(Sprite input)
    {
        slotIcon.sprite = input;
        slotIcon.enabled = true; ;
    }

    public void EmptySlot()
    {
        slotIcon.sprite = null;
        slotIcon.enabled = false;
    }
}