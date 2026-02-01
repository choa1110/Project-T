using UnityEngine;
using Fusion;
using System.Collections.Generic;
using System;

public class RoomPlayer : NetworkBehaviour
{
    public static List<RoomPlayer> Players = new List<RoomPlayer>();
    [Networked] public string NickName { get; set; } // 닉네임 공유
    [Networked] public NetworkBool IsReady { get; set; } // 레디 상태 공유

    // 스폰될 때 리스트에 등록
    public override void Spawned()
    {
        transform.SetParent(GameObject.Find("LobbyManager")?.transform); // 정리용(없어도 됨)
        Players.Add(this);

        // 내 캐릭터라면 닉네임 설정 (여기서는 임시로 Player + 랜덤숫자)
        // 나중에 닉네임 입력받은 걸로 연동 가능
        if (Object.HasInputAuthority)
        {
            RPC_SetNickName("Player " + UnityEngine.Random.Range(1000, 9999));
            IsReady = false; // 처음엔 레디 해제
        }
    }
    // 나갈 때 리스트에서 삭제
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        Players.Remove(this);
    }

    // 닉네임 변경 요청 (RPC: 내 컴퓨터 -> 서버 -> 모든 사람)
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetNickName(string name)
    {
        NickName = name;
    }

    // 레디 상태 변경 요청
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetReady(bool state)
    {
        IsReady = state;
    }
}