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

        rank1Buff.Sort((a, b) => a.buffNum.CompareTo(b.buffNum));
        rank2Buff.Sort((a, b) => a.buffNum.CompareTo(b.buffNum));
        rank3Buff.Sort((a, b) => a.buffNum.CompareTo(b.buffNum));
        itemBuff.Sort((a, b) => a.buffNum.CompareTo(b.buffNum));
    }

    public int GetBuffCount(int rank)
    {
        switch (rank)
        {
            case 1: return rank1Buff != null ? rank1Buff.Count : 0;
            case 2: return rank2Buff != null ? rank2Buff.Count : 0;
            case 3: return rank3Buff != null ? rank3Buff.Count : 0;
            case 4: return itemBuff != null ? itemBuff.Count : 0;
            default: return 0;
        }
    }

    public Buff GetBuff(int rank, int num)
    {
        int count = GetBuffCount(rank);
        if (count == 0)
        {
            if (rank != 1) return GetBuff(1, num);
            return null;
        }
        int safeIndex = Mathf.Clamp(num - 1, 0, count - 1);
        switch (rank)
        {
            case 1: return rank1Buff[safeIndex];
            case 2: return rank2Buff[safeIndex];
            case 3: return rank3Buff[safeIndex];
            case 4: return itemBuff[safeIndex];
            default: return null;
        }
    }

    public Buff GetRank1Buff(int index) { return GetBuff(1, index); }
    public Buff GetRank2Buff(int index) { return GetBuff(2, index); }
    public Buff GetRank3Buff(int index) { return GetBuff(3, index); }
    public Buff GetItemBuff(int index) { return GetBuff(4, index); }
}