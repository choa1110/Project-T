using System.Collections.Generic;
using UnityEngine;

public class BuffDB : MonoBehaviour
{
    static BuffDB _instance;
    public static BuffDB Instance { get => _instance; }

    [SerializeField] List<Buff> rank1Buff;
    [SerializeField] List<Buff> rank2Buff;
    [SerializeField] List<Buff> rank3Buff;
    [SerializeField] List<Buff> itemBuff;

    void Awake()
    {
        if (_instance == null)
            _instance = this;
    }

    public void ApplyItemBuffToPlayer(BuffSystem target, int num)
    {
        target.ApplyBuff(itemBuff[num]);
    }
}