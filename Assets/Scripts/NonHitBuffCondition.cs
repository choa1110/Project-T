using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "Non-Hit Buff Condition", menuName = "Condition Datas / Non-Hit")]
public class NonHitBuffCondition : BuffCondition
{
    public float conditionTime;

    float _lastHitTime;

    public override void Bind(Player player)
    {
        player.onHit.AddListener(IsHit);
        _lastHitTime = Time.time;
    }

    public void IsHit()
    {
        _lastHitTime = Time.time;
    }

    public override bool IsMet()
    {
        return Time.time > _lastHitTime + conditionTime;
    }
}
