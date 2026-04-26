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

    // [Server] 라운드 종료 후 외부(GameManager 등)에서 호출
    public void StartBuffSelectionPhase(int rank)
    {
        if (!Object.HasStateAuthority) return;

        Debug.Log($"[BuffManager] 버프 선택 페이즈 시작 (rank {rank})");

        // 서버가 각 플레이어에게 서로 다른 랜덤 버프를 전송
        foreach (var playerRef in Runner.ActivePlayers)
        {
            int[] options = GetRandomBuffIDs(3, rank);
            RPC_ShowSelectionUI(playerRef, options);
        }
    }

    // [RPC] Server -> Client (UI 표시)
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowSelectionUI([RpcTarget] PlayerRef target, int[] options)
    {
        if (Runner.LocalPlayer == target)
        {
            BuffSelectionUI.Instance.OpenSelection(options);
        }
    }

    // [Client] UI에서 호출 -> 서버로 전달
    public void SendSelectionToServer(int buffID)
    {
        RPC_SelectBuff(buffID);
    }

    // [RPC] Client -> Server (선택 완료, 버프적용)
    [Rpc(RpcSources.InputAuthority | RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_SelectBuff(int buffID, RpcInfo info = default)
    {
        Player senderPlayer = FindPlayerByRef(info.Source);

        if (senderPlayer != null)
        {
            Buff buff = BuffDatabase.Instance.GetBuffByID(buffID);
            if (buff == null) return;

            Debug.Log($"[BuffManager] {info.Source}에서 {buff.buffName} 선택");
            senderPlayer.GetComponent<BuffSystem>().ApplyBuff(buff);
        }
    }

    // PlayerRef로 Player 오브젝트 찾기
    private Player FindPlayerByRef(PlayerRef playerRef)
    {
        foreach (var p in FindObjectsOfType<Player>())
        {
            if (p.GetComponent<NetworkObject>().InputAuthority == playerRef)
                return p;
        }
        return null;
    }

    // rank에 맞는 버프 중 랜덤으로 count개 ID 뽑기 (중복 없음)
    private int[] GetRandomBuffIDs(int count, int rank)
    {
        var allBuffs = BuffDatabase.Instance.allBuffs;

        List<int> validIds = new List<int>();
        for (int i = 0; i < allBuffs.Count; i++)
        {
            if (allBuffs[i].rank == rank)
                validIds.Add(i);
        }

        if (validIds.Count == 0)
        {
            Debug.LogWarning($"[BuffManager] rank {rank} 버프가 BuffDatabase에 없습니다!");
            return new int[0];
        }

        HashSet<int> selected = new HashSet<int>();
        while (selected.Count < count && selected.Count < validIds.Count)
            selected.Add(validIds[Random.Range(0, validIds.Count)]);

        int[] result = new int[selected.Count];
        selected.CopyTo(result);
        return result;
    }
}
