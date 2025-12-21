using System.Collections.Generic;
using UnityEngine;

public enum StatModType
{
    Flat,       // 깡 스탯 (예: +10)
    PercentAdd, // %합연산 (예: +10% -> +0.1 * base)
    Multiplicative // 곱연산 (예: 2배 -> 2.0 * base)
}

[CreateAssetMenu(fileName = "Buff Data", menuName = "Buff Data")]
public class BuffData : ScriptableObject
{
    public string trigger;
    public string buffName;
    public float duration; // 지속 시간 (0 이하면 영구 지속)

    GameObject buffTarget;

    public void Tick(float delta)
    {
        duration = Mathf.Max(0, duration - delta);
    }

    public void ApplyAllEffects(GameObject target)
    {
        buffTarget = target;

        foreach (var effect in effects)
            effect.Apply(target);
    }

    public void ClearAllEffects()
    {
        foreach (var effect in effects)
            effect.Remove();
    }

    [Header("Effects")]
    public List<BuffEffect> effects; // 이 버프가 주는 효과들
}

public abstract class BuffEffect : ScriptableObject
{
    public abstract void Apply(GameObject target);
    public abstract void Remove();
}