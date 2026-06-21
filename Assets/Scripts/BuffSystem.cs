using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class BuffSystem : NetworkBehaviour 
{
    Player _player;
    PlayerStats _playerStats;

    List<BuffFunction> _activeBuffs = new List<BuffFunction>();

    void Awake()
    {
        _player = GetComponent<Player>();
        _playerStats = _player.stats;
    }

    public override void FixedUpdateNetwork()
    {
        for (int i = _activeBuffs.Count - 1; i >= 0; i--)
        {
            _activeBuffs[i].UpdateTick(Runner.DeltaTime);

            if (_activeBuffs[i].expired)
                RemoveBuff(_activeBuffs[i]);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_BroadcastApplyBuff(int rank, int num)
    {
        Buff data = BuffDB.Instance.GetBuff(rank, num);

        if (data == null) return;

        BuffFunction buff = new BuffFunction(data, _player);
        _activeBuffs.Add(buff);
        
        Debug.Log($"[서버] {_player.Object.InputAuthority} 플레이어에게 {data.name} 버프 적용됨!");
    }

    private void RemoveBuff(BuffFunction buff)
    {
        buff.Remove();
        _activeBuffs.Remove(buff);
    }
}