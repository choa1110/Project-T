using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "Buff", menuName = "Buff")]
public class Buff : ScriptableObject
{
    public string buffName;
    public int buffNum;
    public int rank;
    public Sprite icon;

    [TextAreaAttribute]
    public string discription;

    public bool isInfinite;
    public bool isConditional;
    public float duration;

    public List<BuffCondition> conditions;
    public List<BuffEffect> effects;
}

public abstract class BuffCondition : ScriptableObject
{
    public abstract void Bind(Player target);
    public abstract bool IsMet();
}

public abstract class BuffEffect : ScriptableObject
{
    public abstract void Apply(Player target);
    public abstract void Remove();
}