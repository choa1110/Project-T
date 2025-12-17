using System.Collections.Generic;
using UnityEngine;

public class StatBuffSystem : MonoBehaviour
{
    PlayerStats _playerStats;

    List<StatBuff> _permanentBuffs = new List<StatBuff>();
    List<StatBuff> _activeBuffs = new List<StatBuff>();

    void Awake()
    {
        _playerStats = GetComponent<PlayerStats>();
    }

    void Update()
    {
        for (int i = _activeBuffs.Count - 1; i >= 0; i--)
        {
            _activeBuffs[i].Tick(Time.deltaTime);

            if (_activeBuffs[i].durationLeft < 0)
                RemoveBuff(_activeBuffs[i]);
        }
    }

    public void ApplyBuff(StatBuffData buffData)
    {
        StatBuff newBuff = new StatBuff(buffData);

        foreach (var effect in buffData.effects)
        {
            Stat statToModify = _playerStats.GetStat(effect.targetStat);
            if (statToModify != null)
            {
                StatModifier mod = new StatModifier(effect.amount, effect.modType, newBuff);

                statToModify.AddModifier(mod);

                if (buffData.duration > 0)
                {
                    newBuff.registeredModifiers.Add(statToModify, mod);
                    _activeBuffs.Add(newBuff);
                }
                else
                    _permanentBuffs.Add(newBuff);
            }
        }
    }

    private void RemoveBuff(StatBuff buff)
    {
        // 등록된 모든 Modifier 제거
        foreach (var kvp in buff.registeredModifiers)
        {
            Stat targetStat = kvp.Key;
            StatModifier mod = kvp.Value;

            targetStat.RemoveModifier(mod);
        }

        _activeBuffs.Remove(buff);
    }
}

public class StatBuff
{
    StatBuffData buffData;
    public float durationLeft { get; private set; }

    // 나중에 제거하기 위해 Modifier를 줬는지 기억
    public Dictionary<Stat, StatModifier> registeredModifiers = new Dictionary<Stat, StatModifier>();

    public StatBuff(StatBuffData data)
    {
        buffData = data;
        durationLeft = data.duration;
    }

    public void Tick(float delta)
    {
        durationLeft -= delta;
    }
}