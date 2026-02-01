using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "Item", menuName = "Item")]
public class Item : ScriptableObject
{
    public int itemId;
    public string itemName;
    public Sprite itemIcon;

    [TextArea]
    public string description;
}