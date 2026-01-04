using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "OnHit Buff Condition", menuName = "Condition Datas / OnHit")]
public class OnHitBuffCondition : BuffCondition
{
    public float duration;

    float _lastHitTime = -10f;

    public override void Bind(Player player)
    {
        player.onHit.AddListener(IsHit);
    }

    public void IsHit()
    {
        _lastHitTime = Time.time;
    }

    public override bool IsMet()
    {
        return Time.time < _lastHitTime + duration;
    }
}
