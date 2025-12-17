using System.Collections.Generic;
using UnityEngine;

public enum StatType
{
    MaxHP,
    PowDam,
    PowKnock,
    AtkSpeed,
    AtkRange,
    SpeedMove,
    SpeedSprint,
    StaDrainRate,
    JumpHeight,
    Weight
}

public class PlayerStats
{
    [SerializeField] private Stat _maxHP;
    [SerializeField] private Stat _powDam;
    [SerializeField] private Stat _powKnock;
    [SerializeField] private Stat _atkSpeed;
    [SerializeField] private Stat _atkRange;
    [SerializeField] private Stat _speedMove;
    [SerializeField] private Stat _speedSprint;
    [SerializeField] private Stat _staDrainRate; // 스태미나 소모율
    [SerializeField] private Stat _jumpHeight;
    [SerializeField] private Stat _weight; // 넉백 저항

    Dictionary<StatType, Stat> _statDictionary;

    private void InitializeStatDictionary()
    {
        _statDictionary = new Dictionary<StatType, Stat>()
        {
            { StatType.MaxHP, _maxHP },
            { StatType.PowDam, _powDam },
            { StatType.PowKnock, _powKnock },
            { StatType.AtkSpeed, _atkSpeed },
            { StatType.AtkRange, _atkRange },
            { StatType.SpeedMove, _speedMove },
            { StatType.SpeedSprint, _speedSprint },
            { StatType.StaDrainRate, _staDrainRate },
            { StatType.JumpHeight, _jumpHeight },
            { StatType.Weight, _weight }
        };
    }

    public Stat GetStat(StatType type)
    {
        if (_statDictionary.TryGetValue(type, out Stat stat))
            return stat;

        Debug.LogError($"StatType {type}을 찾을 수 없습니다.");
        return null;
    }
}