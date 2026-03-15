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

    public Buff GetRank1Buff(int index) { return rank1Buff[index]; }
    public Buff GetRank2Buff(int index) { return rank2Buff[index]; }
    public Buff GetRank3Buff(int index) { return rank3Buff[index]; }
    public Buff GetItemBuff(int index) { return itemBuff[index]; }

    // 라운드(Rank)에 맞는 전체 리스트 반환
    public List<Buff> GetBuffListByRank(int rank)
    {
        switch (rank)
        {
            case 1: return rank1Buff;
            case 2: return rank2Buff;
            case 3: return rank3Buff;
            default: return rank1Buff; // 예외 상황 방지용 기본값
        }
    }
}