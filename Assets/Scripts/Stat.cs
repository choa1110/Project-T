using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Stat
{
    [SerializeField] float _baseValue;

    float _totalValue;

    readonly List<StatModifier> _modifiers;

    public float BaseValue { get => _baseValue; set { _baseValue = value; _totalValue = CalculateFinalValue(); } }
    public float Value { get => _totalValue; }

    // 생성자
    public Stat(float baseValue)
    {
        _baseValue = baseValue;
        _modifiers = new List<StatModifier>();
    }

    // 버프 추가
    public void AddModifier(StatModifier mod)
    {
        _modifiers.Add(mod);
        _totalValue = CalculateFinalValue();
    }

    // 버프 제거
    public void RemoveModifier(StatModifier mod)
    {
        _modifiers.Remove(mod);
        _totalValue = CalculateFinalValue();
    }

    // 특정 출처의 모든 버프 제거
    public void RemoveAllModifiersFromSource(object source)
    {
        for (int i = _modifiers.Count - 1; i >= 0; i--)
        {
            if (_modifiers[i].source == source)
                _modifiers.RemoveAt(i);
        }

        _totalValue = CalculateFinalValue();
    }

    // 연산 순서: 곱연산 -> %합연산(기본값 기반) -> 합연산
    private float CalculateFinalValue()
    {
        float finalValue = _baseValue;
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
        return valueAfterMult + valueFromPercent + sumFlatAdd;
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