using System.Collections.Generic;
using UnityEngine;

public class BuffSystem : MonoBehaviour
{
    Player _player;
    PlayerStats _playerStats;

    List<BuffFunction> _activeBuffs = new List<BuffFunction>();

    void Awake()
    {
        _player = GetComponent<Player>();
        _playerStats = _player.stats;
    }

    void Update()
    {
        for (int i = _activeBuffs.Count - 1; i >= 0; i--)
        {
            _activeBuffs[i].UpdateTick(Time.deltaTime);

            if (_activeBuffs[i].expired)
                RemoveBuff(_activeBuffs[i]);
        }
    }

    public void ApplyBuff(Buff data)
    {
        BuffFunction buff = new BuffFunction(data, _player);

        _activeBuffs.Add(buff);
    }

    private void RemoveBuff(BuffFunction buff)
    {
        buff.Remove();
        _activeBuffs.Remove(buff);
    }
}