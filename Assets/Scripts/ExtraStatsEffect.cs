using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "Extra Stats Effect", menuName = "Effect Datas/Extra Stats")]
public class ExtraStatsEffect : BuffEffect
{
    public ExtraStatType targetStat;
    public int amount;

    Player targetPlayer;

    public override void Apply(Player target)
    {
        targetPlayer = target;
        targetPlayer.ExtraStatModify(targetStat, amount);
    }

    public override void Remove()
    {
        targetPlayer.ExtraStatModify(targetStat, -amount);
    }
}
