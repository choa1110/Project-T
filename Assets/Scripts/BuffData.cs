using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "Buff Data", menuName = "Buff Data")]
public class BuffData : ScriptableObject
{
    public string buffName;
    public int rank;

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