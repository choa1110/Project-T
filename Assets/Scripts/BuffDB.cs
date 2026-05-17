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

    public Buff GetBuff(int rank, int num)
    {
        switch (rank)
        {
            case 1:
                return rank1Buff[num - 1];
            case 2:
                return rank2Buff[num - 1];
            case 3:
                return rank3Buff[num - 1];
            case 4:
                return itemBuff[num - 1];
            default:
                return null;
        }
    }

    public Buff GetRank1Buff(int index) { return rank1Buff[index - 1]; }
    public Buff GetRank2Buff(int index) { return rank2Buff[index - 1]; }
    public Buff GetRank3Buff(int index) { return rank3Buff[index - 1]; }
    public Buff GetItemBuff(int index) { return itemBuff[index - 1]; }
}