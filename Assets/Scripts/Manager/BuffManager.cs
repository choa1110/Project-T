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
    // [Server] 서버 시작 시 외부(GameManager 등)에서 호출
    // ====================================================
    public void StartBuffSelectionPhase()
    {
        if (!Object.HasStateAuthority) return; // 서버만 실행 가능

        Debug.Log("[BuffManager] 버프 선택 페이즈 시작");

        // 현재 라운드 가져오기 (GameManager가 없으면 기본값 1)
        int currentRound = GameManager.instance != null ? GameManager.instance.CurrentRound : 1;

        // 접속한 모든 플레이어에게 각각 다른 무작위 선택지 제공
        foreach (var playerRef in Runner.ActivePlayers)
        {
            int[] options = GetRandomBuffIDs(3, currentRound);
            RPC_ShowSelectionUI(playerRef, options, currentRound);
        }
    }

    // ====================================================
    // [RPC] Server -> Client (UI 띄우기)
    // ====================================================
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowSelectionUI([RpcTarget] PlayerRef target, int[] options, int round)
    {
        // 각 클라이언트(Local Player)에서 이 메시지를 받으면 UI 표시
        if (Runner.LocalPlayer == target)
        {
            BuffSelectionUI.Instance.OpenSelection(options, round);
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
    // [RPC] Client -> Server (선택 완료, 능력치 적용)
    // ====================================================
    [Rpc(RpcSources.InputAuthority | RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_SelectBuff(int buffID, RpcInfo info = default)
    {
        // 1. RPC를 보낸 플레이어 찾기
        Player senderPlayer = FindPlayerByRef(info.Source);

        if (senderPlayer != null)
        {
            // 현재 라운드를 기준으로 버프 리스트를 가져와서 해당 버프를 찾음
            int currentRound = GameManager.instance != null ? GameManager.instance.CurrentRound : 1;
            List<Buff> targetList = BuffDB.Instance.GetBuffListByRank(currentRound);

            // 유효한 인덱스인지 검사
            if (buffID >= 0 && buffID < targetList.Count)
            {
                Buff buff = targetList[buffID];
                Debug.Log($"[BuffManager] {info.Source}에서 {buff.buffName} 선택됨");

                // 2. 실제 버프 적용 (해당 플레이어의 버프시스템)
                senderPlayer.GetComponent<BuffSystem>().ApplyBuff(buff);
            }
        }
    }

    // ----------------------------------------------------
    // 유틸리티 함수들
    // ----------------------------------------------------

    // PlayerRef를 통해 Player 객체 찾기 (로컬 검색)
    private Player FindPlayerByRef(PlayerRef playerRef)
    {
        foreach (var p in FindObjectsOfType<Player>())
        {
            if (p.GetComponent<NetworkObject>().InputAuthority == playerRef)
                return p;
        }
        return null;
    }

    // 중복 없는 무작위 ID 뽑기 (현재 라운드 기준)
    private int[] GetRandomBuffIDs(int count, int round)
    {
        List<Buff> targetList = BuffDB.Instance.GetBuffListByRank(round);
        if (targetList == null || targetList.Count == 0) return new int[0];

        HashSet<int> selected = new HashSet<int>();
        while (selected.Count < count && selected.Count < targetList.Count)
        {
            selected.Add(Random.Range(0, targetList.Count));
        }

        int[] result = new int[selected.Count];
        selected.CopyTo(result);
        return result;
    }
}