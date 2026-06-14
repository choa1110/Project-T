using System.Collections.Generic;
using UnityEngine;

public class AbilityDB : MonoBehaviour
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
        user.ApplyHeal(0.2f);
    }

    // Rpc Request
}