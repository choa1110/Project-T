using UnityEngine;
using Fusion;
using System.Collections.Generic;
using System;

public class RoomPlayer : NetworkBehaviour
{
    public static List<RoomPlayer> Players = new List<RoomPlayer>();
    [Networked] public string NickName { get; set; } // 닉네임 공유
    [Networked] public NetworkBool IsReady { get; set; } // 레디 상태 공유
    [Networked] public NetworkBool IsLeader { get; set; } // 방장 정보
    
    public override void Spawned()
    {
        transform.SetParent(GameObject.Find("LobbyManager")?.transform); // 정리용(없어도 됨)
        Players.Add(this);

        if (Object.HasInputAuthority)
        {
            string myName = "Player " + UnityEngine.Random.Range(1000, 9999);
            if (DataManager.Instance != null && !string.IsNullOrEmpty(DataManager.Instance.UserNickName))
            {
                 myName = DataManager.Instance.UserNickName;
            }
            RPC_SetNickName(myName);
            IsReady = false; // 처음엔 레디 해제
            if (Runner.IsServer)
            {
                IsLeader = true;
            }
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