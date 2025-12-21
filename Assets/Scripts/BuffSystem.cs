using System.Collections.Generic;
using UnityEngine;

public class BuffSystem : MonoBehaviour
{
    PlayerStats _playerStats;

    List<BuffData> _permanentBuffs = new List<BuffData>();
    List<BuffData> _activeBuffs = new List<BuffData>();

    void Awake()
    {
        _playerStats = GetComponent<Player>().stats;
    }

    void Update()
    {
        for (int i = _activeBuffs.Count - 1; i >= 0; i--)
        {
            _activeBuffs[i].Tick(Time.deltaTime);

            if (Mathf.Approximately(_activeBuffs[i].duration, 0))
                RemoveBuff(_activeBuffs[i]);
        }
    }

    public void ApplyBuff(BuffData buffData)
    {
        BuffData buff = Instantiate(buffData);

        if (buffData.duration > 0)
            _activeBuffs.Add(buff);
        else
            _permanentBuffs.Add(buff);

        buff.ApplyAllEffects(gameObject);
    }

    private void RemoveBuff(BuffData buff)
    {
        buff.ClearAllEffects();
        _activeBuffs.Remove(buff);
    }
}