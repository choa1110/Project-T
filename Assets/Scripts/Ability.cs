using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "Ability", menuName = "Ability")]
public class Ability : ScriptableObject
{
    public string skillName;
    public int skillNum;
    public Sprite icon;

    public float coolTime;
    public float duration;
}