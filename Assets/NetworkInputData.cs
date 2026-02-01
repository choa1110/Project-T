using Fusion;
using UnityEngine;

// 입력값을 담을 그릇 (버튼, 방향 등)
public struct NetworkInputData : INetworkInput
{
    public Vector2 direction; // 이동 방향 (x, y)
}