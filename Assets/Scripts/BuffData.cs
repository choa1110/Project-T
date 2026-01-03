using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Buff Data", menuName = "Buff Data/Default")]
public class BuffData : ScriptableObject
{
    public string buffName;
    public float buffDuration; // 지속 시간 (0 이하면 영구 지속)
    public BuffCondition condition = BuffCondition.Always;

    protected Player buffTarget;

    public void Tick(float delta)
    {
        buffDuration = Mathf.Max(0, buffDuration - delta);
    }

    public void ApplyAllEffects(Player target)
    {
        buffTarget = target;

        foreach (var effect in effects)
            effect.Apply(buffTarget);
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
    public abstract void Apply(Player target);
    public abstract void Remove();
}