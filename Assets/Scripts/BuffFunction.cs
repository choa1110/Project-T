using System;
using UnityEngine;

[Serializable]
public class BuffFunction
{
    Buff _buff;
    Player _target;
    BuffSystem _bs;

    public bool expired { get; private set; }

    float timeLeft;
    bool isConditionMet;

    public BuffFunction(Buff data, Player player, BuffSystem bs)
    {
        _buff = data;
        _target = player;
        _bs = bs;

        timeLeft = _buff.duration;

        foreach (var cond in _buff.conditions)
            cond.Bind(_target);
    }

    public void UpdateTick(float delta, int index)
    {
        if (!_buff.isInfinite)
        {
            timeLeft = Mathf.Max(0, timeLeft - delta);

            if (Mathf.Approximately(timeLeft, 0))
                expired = true;
        }

        bool nowMet = true;
        foreach (var cond in _buff.conditions)
        {
            if (!cond.IsMet())
            {
                nowMet = false; 
                break;
            }
        }

        if (nowMet && !isConditionMet)
            _bs.Rpc_BroadcastApplyEffect(index);
        else if (!nowMet && isConditionMet)
            _bs.Rpc_BroadcastRemoveEffect(index);

        isConditionMet = nowMet;
    }

    public void ApplyEffectLocal() => _buff.effects.ForEach(e => e.Apply(_target));
    public void RemoveEffectLocal() => _buff.effects.ForEach(e => e.Remove());
}