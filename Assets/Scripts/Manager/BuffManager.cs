using Fusion;
using UnityEngine;
using System.Collections.Generic;

public class BuffManager : NetworkBehaviour
{
    public static BuffManager Instance;

    void Awake()
    {
        Instance = this;
    }

    // ====================================================
    // [Server] 라운드 종료 시 외부(GameManager 등)에서 호출
    // ====================================================
    public void StartBuffSelectionPhase()
    {
        if (!Object.HasStateAuthority) return; // 서버만 실행 가능

        Debug.Log("[BuffManager] 버프 선택 시작");

        // 접속한 모든 플레이어에게 각자 다른 랜덤 선택지 전송
        foreach (var playerRef in Runner.ActivePlayers)
        {
            int[] options = GetRandomBuffIDs(3);
            RPC_ShowSelectionUI(playerRef, options);
        }
    }

    // ====================================================
    // [RPC] Server -> Client (UI 열어라)
    // ====================================================
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowSelectionUI([RpcTarget] PlayerRef target, int[] options)
    {
        // 내 클라이언트(Local Player)에게 온 메시지일 때만 UI 표시
        if (Runner.LocalPlayer == target)
        {
            BuffSelectionUI.Instance.OpenSelection(options);
        }
    }

    // ====================================================
    // [Client] UI에서 호출 -> 서버로 전송
    // ====================================================
    public void SendSelectionToServer(int buffID)
    {
        RPC_SelectBuff(buffID);
    }

    // ====================================================
    // [RPC] Client -> Server (선택 완료, 적용해줘)
    // ====================================================
    [Rpc(RpcSources.InputAuthority | RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_SelectBuff(int buffID, RpcInfo info = default)
    {
        // 1. RPC를 보낸 플레이어 찾기
        Player senderPlayer = FindPlayerByRef(info.Source);

        if (senderPlayer != null)
        {
            BuffData buff = BuffDatabase.Instance.GetBuffByID(buffID);
            Debug.Log($"[BuffManager] {info.Source}님이 {buff.buffName} 선택함");

            // 2. 실제 버프 적용 (서버 권한으로 실행됨)
            senderPlayer.GetComponent<BuffSystem>().ApplyBuff(buff);
        }
    }

    // ----------------------------------------------------
    // 유틸리티 함수들
    // ----------------------------------------------------

    // PlayerRef로 실제 Player 객체 찾기 (간단 버전)
    private Player FindPlayerByRef(PlayerRef playerRef)
    {
        foreach (var p in FindObjectsOfType<Player>())
        {
            if (p.GetComponent<NetworkObject>().InputAuthority == playerRef)
                return p;
        }
        return null;
    }

    // 중복 없는 랜덤 ID 뽑기
    private int[] GetRandomBuffIDs(int count)
    {
        var allBuffs = BuffDatabase.Instance.allBuffs;
        if (allBuffs.Count == 0) return new int[0];

        HashSet<int> selected = new HashSet<int>();
        while (selected.Count < count && selected.Count < allBuffs.Count)
        {
            selected.Add(Random.Range(0, allBuffs.Count));
        }

        int[] result = new int[selected.Count];
        selected.CopyTo(result);
        return result;
    }
}