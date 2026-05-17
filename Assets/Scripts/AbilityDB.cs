using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class AbilityDB : NetworkBehaviour
{
    static AbilityDB _instance;
    public static AbilityDB Instance { get => _instance; }

    [SerializeField] List<Ability> abilityList;

    void Awake()
    {
        if (_instance == null)
            _instance = this;

        abilityList.Sort((a, b) => a.skillNum.CompareTo(b.skillNum));
    }

    public Ability SetAbility(int num)
    {
        if (num < 1 || num > abilityList.Count)
            return null;

        return abilityList[num - 1];
    }

    public void ActivateAbility(int num, Player user)
    {
        switch (num)
        {
            case 1:
                OnUse_HealingHands(user);
                break;
        }

        user.Rpc_BroadcastSkillActivate();
    }

    void OnUse_HealingHands(Player user)
    {
        user.Rpc_BroadcastHeal(50);
        //Rpc_RequestHealToServer(user, 50);
    }

    // Rpc Request
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    void Rpc_RequestHealToServer(Player user, int amount)
    {
        user.Rpc_BroadcastHeal(amount);
    }
}