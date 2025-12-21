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

[System.Serializable]
public class PlayerStats
{
    [Header("Base Stats")]
    [SerializeField] float maxHP;
    [SerializeField] float powDam;
    [SerializeField] float powKnock;
    [SerializeField] float atkSpeed;
    [SerializeField] float atkRange;
    [SerializeField] float speedMove;
    [SerializeField] float speedSprint;
    [SerializeField] float staDrainRate; // 스태미나 소모율
    [SerializeField] float jumpHeight;
    [SerializeField] float weight; // 넉백 저항

    private Stat _maxHP;
    private Stat _powDam;
    private Stat _powKnock;
    private Stat _atkSpeed;
    private Stat _atkRange;
    private Stat _speedMove;
    private Stat _speedSprint;
    private Stat _staDrainRate;
    private Stat _jumpHeight;
    private Stat _weight;

    Dictionary<StatType, Stat> _statDictionary;

    public void InitalizeStats()
    {
        _maxHP = new Stat(maxHP);
        _powDam = new Stat(powDam);
        _powKnock = new Stat(powKnock);
        _atkSpeed = new Stat(atkSpeed);
        _atkRange = new Stat(atkRange);
        _speedMove = new Stat(speedMove);
        _speedSprint = new Stat(speedSprint);
        _staDrainRate = new Stat(staDrainRate);
        _jumpHeight = new Stat(jumpHeight);
        _weight = new Stat(weight);

        InitializeStatDictionary();
    }

    public void InitializeStatDictionary()
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