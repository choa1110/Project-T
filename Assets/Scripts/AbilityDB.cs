using UnityEngine;

public class AbilityDB : MonoBehaviour
{
    static AbilityDB _instance;
    public static AbilityDB Instance { get => _instance; }

    void Awake()
    {
        if (_instance == null)
            _instance = this;
    }

    public void ActivateAbility(int num, Player user)
    {
        switch (num)
        {
            case 0:

                break;
            case 1:
                OnUse_HealingHands(user);
                break;
        }
    }

    void OnUse_HealingHands(Player user)
    {
        user.ApplyHeal(100);
    }
}