using System.Collections.Generic;
using UnityEngine;

public class BuffSystem : MonoBehaviour
{
    Player _player;
    PlayerStats _playerStats;

    List<BuffData> _permanentBuffs = new List<BuffData>();
    List<BuffData> _activeBuffs = new List<BuffData>();

    void Awake()
    {
        _player = GetComponent<Player>();
        _playerStats = _player.stats;
    }

    void Update()
    {
        for (int i = _activeBuffs.Count - 1; i >= 0; i--)
        {
            _activeBuffs[i].Tick(Time.deltaTime);

            if (Mathf.Approximately(_activeBuffs[i].buffDuration, 0))
                RemoveBuff(_activeBuffs[i]);
        }
    }

    public void ApplyBuff(BuffData buffData)
    {
        BuffData buff = Instantiate(buffData);

        if (buffData.buffDuration > 0)
            _activeBuffs.Add(buff);
        else
            _permanentBuffs.Add(buff);

        switch (buffData.condition) {
            case BuffCondition.Always:
                buff.ApplyAllEffects(_player);
                break;
            case BuffCondition.OnHit:
                ConditionalBuffData conditBuffData = (ConditionalBuffData)buffData;
                conditBuffData.SetCondition(this, _player.onHit);
                break;
        }
    }

    private void RemoveBuff(BuffData buff)
    {
        buff.ClearAllEffects();
        _activeBuffs.Remove(buff);
    }
}