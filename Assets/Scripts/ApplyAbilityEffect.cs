using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "Apply Ability Effect", menuName = "Effect Datas/Apply Ability")]
public class ApplyAbilityEffect : BuffEffect
{
    public Ability ability;

    Player targetPlayer;

    public override void Apply(Player target)
    {
        targetPlayer = target;

        targetPlayer.Rpc_RequestSetAbility(ability.skillNum);
    }

    public override void Remove() {}
}