using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class BuffSystem : NetworkBehaviour 
{
    Player _player;
    PlayerStats _playerStats;

    List<BuffFunction> _activeBuffs = new List<BuffFunction>();
    List<string> _activeBuffNames   = new List<string>();

    void Awake()
    {
        _player = GetComponent<Player>();
        _playerStats = _player.stats;
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        for (int i = _activeBuffs.Count - 1; i >= 0; i--)
        {
            _activeBuffs[i].UpdateTick(Runner.DeltaTime);

            if (_activeBuffs[i].expired)
                RemoveBuff(_activeBuffs[i]);
        }
    }

    public void ApplyBuff(Buff data)
    {
        if (data == null) return;

        BuffFunction buff = new BuffFunction(data, _player);
        _activeBuffs.Add(buff);
        _activeBuffNames.Add(data.buffName);
        
        Debug.Log($"[서버] {_player.Object.InputAuthority} 플레이어에게 {data.name} 버프 적용됨!");
    }

    private void RemoveBuff(BuffFunction buff)
    {
        int idx = _activeBuffs.IndexOf(buff);
        buff.Remove();
        _activeBuffs.Remove(buff);
        if (idx >= 0 && idx < _activeBuffNames.Count)
            _activeBuffNames.RemoveAt(idx);
    }

    public List<string> GetActiveBuffNames()
    {
        return new List<string>(_activeBuffNames);
    }
}
