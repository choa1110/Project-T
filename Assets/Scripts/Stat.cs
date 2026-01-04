using System.Collections.Generic;

public enum StatModType
{
    Flat,       // 깡 스탯 (예: +10)
    PercentAdd, // %합연산 (예: +10% -> +0.1 * base)
    Multiplicative // 곱연산 (예: 2배 -> 2.0 * base)
}

public class Stat
{
    float _baseValue;
    float _totalValue;

    public readonly List<StatModifier> _modifiers;

    public float BaseValue { get => _baseValue; set { _baseValue = value; CalculateFinalValue(); } }
    public float Value { get => _totalValue; }

    // 생성자
    public Stat(float baseValue)
    {
        _baseValue = baseValue;
        _totalValue = baseValue;
        _modifiers = new List<StatModifier>();
    }

    // 버프 추가
    public void AddModifier(StatModifier mod)
    {
        _modifiers.Add(mod);
        CalculateFinalValue();
    }

    // 버프 제거
    public void RemoveModifier(StatModifier mod)
    {
        _modifiers.Remove(mod);
        CalculateFinalValue();
    }

    // 특정 출처의 모든 버프 제거
    public void RemoveAllModifiersFromSource(object source)
    {
        for (int i = _modifiers.Count - 1; i >= 0; i--)
        {
            if (_modifiers[i].source == source)
                _modifiers.RemoveAt(i);
        }

        CalculateFinalValue();
    }

    // 연산 순서: 곱연산 -> %합연산(기본값 기반) -> 합연산
    void CalculateFinalValue()
    {
        float sumPercentAdd = 0;
        float sumFlatAdd = 0;
        float totalMultiplier = 1;

        for (int i = 0; i < _modifiers.Count; i++)
        {
            StatModifier mod = _modifiers[i];

            if (mod.type == StatModType.Multiplicative)
            {
                // 곱연산들을 모두 곱함
                totalMultiplier *= mod.amount;
            }
            else if (mod.type == StatModType.PercentAdd)
            {
                // %합연산들을 모두 더함
                sumPercentAdd += mod.amount;
            }
            else if (mod.type == StatModType.Flat)
            {
                sumFlatAdd += mod.amount;
            }
        }

        float valueAfterMult = _baseValue * totalMultiplier;
        float valueFromPercent = _baseValue * sumPercentAdd;

        // 최종 공식: (기본값 * 곱연산) + (기본값 * %합연산) + 합연산
        _totalValue = valueAfterMult + valueFromPercent + sumFlatAdd;
    }
}

public class StatModifier
{
    public float amount;
    public StatModType type;
    public object source;

    public StatModifier(float am, StatModType tp, object so)
    {
        amount = am;
        type = tp;
        source = so;
    }
}