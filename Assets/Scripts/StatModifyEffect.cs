using UnityEngine;

public enum StatModType
{
    Flat,       // ±ø ½ºÅÈ (¿¹: +10)
    PercentAdd, // %ÇÕ¿¬»ê (¿¹: +10% -> +0.1 * base)
    Multiplicative // °ö¿¬»ê (¿¹: 2¹è -> 2.0 * base)
}

[System.Serializable]
[CreateAssetMenu(fileName = "Stat Modify Effect", menuName = "Effect Datas/Stat Modify")]
public class StatModifyEffect : BuffEffect
{
    public StatType targetStat;
    public StatModType modType;
    public float amount;

    PlayerStats playerStats;

    public override void Apply(Player target)
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