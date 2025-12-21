using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "Stat Modify Effect", menuName = "Effect Datas/Stat Modify")]
public class StatModifyEffect : BuffEffect
{
    public StatType targetStat;
    public StatModType modType;
    public float amount;

    PlayerStats playerStats;

    public override void Apply(GameObject target)
    {
        playerStats = target.GetComponent<Player>().stats;

        Stat statToModify = playerStats.GetStat(targetStat);
        StatModifier mod = new StatModifier(amount, modType, this);
        statToModify.AddModifier(mod);
    }

    public override void Remove()
    {
        Stat statToModify = playerStats.GetStat(targetStat);
        statToModify.RemoveAllModifiersFromSource(this);
    }
}