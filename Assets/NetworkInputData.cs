using Fusion;
using UnityEngine;

// 1. 어떤 버튼들이 있는지 목록을 만듭니다. (점프, 공격 등)
public enum InputButtons
{
    Jump = 0,
    Attack = 1,
    // 필요한 다른 키가 있다면 여기에 추가 (예: Skill1)
}

public struct NetworkInputData : INetworkInput
{
    public Vector2 direction; // 이동 방향

    public NetworkButtons buttons; 
}