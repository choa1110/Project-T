using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class BuffSystem : NetworkBehaviour 
{
    [SerializeField] Player _player;

    public List<BuffFunction> _appliedBuffs = new List<BuffFunction>();
    List<Buff> _activeBuffData = new List<Buff>();

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        for (int i = _appliedBuffs.Count - 1; i >= 0; i--)
        {
            _appliedBuffs[i].UpdateTick(Runner.DeltaTime, i);

            if (_appliedBuffs[i].expired)
                Rpc_BroadcastRemoveBuff(i);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_BroadcastApplyBuff(int rank, int num)
    {
        Buff data = BuffDB.Instance.GetBuff(rank, num);

        if (data == null) return;

        BuffFunction buff = new BuffFunction(data, _player, this);
        _appliedBuffs.Add(buff);
        
        Debug.Log($"[서버] {_player.Object.InputAuthority} 플레이어에게 {data.name} 버프 적용됨!");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void Rpc_BroadcastRemoveBuff(int num)
    {
        _appliedBuffs[num].RemoveEffectLocal();
        _appliedBuffs.RemoveAt(num);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_BroadcastApplyEffect(int index)
    {
        if (index >= 0 && index < _appliedBuffs.Count)
            _appliedBuffs[index].ApplyEffectLocal();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_BroadcastRemoveEffect(int index)
    {
        if (index >= 0 && index < _appliedBuffs.Count)
            _appliedBuffs[index].RemoveEffectLocal();
    }

    public List<Buff> GetActiveBuffs()
    {
        return new List<Buff>(_activeBuffData);
    }
}