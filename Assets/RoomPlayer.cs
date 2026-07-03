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
    [Networked] public int Team { get; set; } // 팀 정보 (0: RED, 1: BLUE)
    
    public override void Spawned()
    {
        Players.Add(this);

        if (Object.HasStateAuthority)
        {
            // Default Team assignment based on join order (first two -> RED (0), next two -> BLUE (1))
            Team = (Players.Count <= 2) ? 0 : 1;
        }

        if (Object.HasInputAuthority)
        {
            string myName = DataManager.Instance.UserNickName;
            
            // If no saved nickname, generate a random one and save it
            if (string.IsNullOrEmpty(myName))
            {
                myName = "Player " + UnityEngine.Random.Range(1000, 9999);
                DataManager.Instance.SetNickName(myName);
            }

            Debug.Log($"[RoomPlayer] Using Nickname: {myName}");
            RPC_SetNickName(myName);
            
            IsReady = false; 
            if (Runner.IsServer)
            {
                IsLeader = true;
            }

            // Connect to Chat after nickname is set
            FindFirstObjectByType<ChatManager>()?.ConnectWithNickName();
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

    // 팀 변경 요청
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_ToggleTeam()
    {
        Team = (Team == 0) ? 1 : 0;
        Debug.Log($"[RoomPlayer] {NickName} toggled team to {Team}");
    }
}