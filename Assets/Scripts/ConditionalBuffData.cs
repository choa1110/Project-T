using UnityEngine;
using UnityEngine.Events;

public enum BuffCondition
{
    Always,
    OnHit
}

[CreateAssetMenu(fileName = "Conditional Buff Data", menuName = "Buff Data/Conditional")]
public class ConditionalBuffData : BuffData
{
    public BuffData onCondition;

    BuffSystem _system;

    public void SetCondition(BuffSystem system, UnityEvent even)
    {
        _system = system;

        even.AddListener(ApplyBuff);
    }

    public void ApplyBuff()
    {
        _system.ApplyBuff(onCondition);
    }
}
