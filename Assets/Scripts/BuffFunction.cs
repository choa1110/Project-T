using UnityEngine;

public class BuffFunction
{
    Buff _buff;
    Player _target;

    public bool expired { get; private set; }

    float timeLeft;
    bool isConditionMet;

    public BuffFunction(Buff data, Player player)
    {
        _buff = data;
        _target = player;
        timeLeft = data.duration;

        foreach (var cond in _buff.conditions)
            cond.Bind(_target);
    }

    public void UpdateTick(float delta)
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
            Apply();
        else if (!nowMet && isConditionMet)
            Remove();

        isConditionMet = nowMet;
    }

    private void Apply() => _buff.effects.ForEach(e => e.Apply(_target));
    public void Remove() => _buff.effects.ForEach(e => e.Remove());
}