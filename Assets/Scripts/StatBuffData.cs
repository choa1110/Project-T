using System.Collections.Generic;
using UnityEngine;

public enum StatModType
{
    Flat,       // 깡 스탯 (예: +10)
    PercentAdd, // %합연산 (예: +10% -> +0.1 * base)
    Multiplicative // 곱연산 (예: 2배 -> 2.0 * base)
}

[CreateAssetMenu(fileName = "Stat Buff Data", menuName = "Buff Datas/Stat Buff")]
public class StatBuffData : ScriptableObject
{
    [System.Serializable]
    public struct BuffEffect
    {
        public StatType targetStat;
        public StatModType modType;
        public float amount;
    }

    public string buffName;
    public float duration; // 지속 시간 (0 이하면 영구 지속)

    [Header("Effects")]
    public List<BuffEffect> effects; // 이 버프가 주는 효과들
}