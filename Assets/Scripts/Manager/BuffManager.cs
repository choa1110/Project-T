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
    // [Server] ���� ���� �� �ܺ�(GameManager ��)���� ȣ��
    // ====================================================
    public void StartBuffSelectionPhase()
    {
        if (!Object.HasStateAuthority) return; // ������ ���� ����

        Debug.Log("[BuffManager] ���� ���� ����");

        // ������ ��� �÷��̾�� ���� �ٸ� ���� ������ ����
        foreach (var playerRef in Runner.ActivePlayers)
        {
            int[] options = GetRandomBuffIDs(3);
            RPC_ShowSelectionUI(playerRef, options);
        }
    }

    // ====================================================
    // [RPC] Server -> Client (UI �����)
    // ====================================================
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowSelectionUI([RpcTarget] PlayerRef target, int[] options)
    {
        // �� Ŭ���̾�Ʈ(Local Player)���� �� �޽����� ���� UI ǥ��
        if (Runner.LocalPlayer == target)
        {
            BuffSelectionUI.Instance.OpenSelection(options);
        }
    }

    // ====================================================
    // [Client] UI���� ȣ�� -> ������ ����
    // ====================================================
    public void SendSelectionToServer(int buffID)
    {
        RPC_SelectBuff(buffID);
    }

    // ====================================================
    // [RPC] Client -> Server (���� �Ϸ�, ��������)
    // ====================================================
    [Rpc(RpcSources.InputAuthority | RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_SelectBuff(int buffID, RpcInfo info = default)
    {
        // 1. RPC�� ���� �÷��̾� ã��
        Player senderPlayer = FindPlayerByRef(info.Source);

        if (senderPlayer != null)
        {
            Buff buff = BuffDatabase.Instance.GetBuffByID(buffID);
            Debug.Log($"[BuffManager] {info.Source}���� {buff.buffName} ������");

            // 2. ���� ���� ���� (���� �������� �����)
            senderPlayer.GetComponent<BuffSystem>().ApplyBuff(buff);
        }
    }

    // ----------------------------------------------------
    // ��ƿ��Ƽ �Լ���
    // ----------------------------------------------------

    // PlayerRef�� ���� Player ��ü ã�� (���� ����)
    private Player FindPlayerByRef(PlayerRef playerRef)
    {
        foreach (var p in FindObjectsOfType<Player>())
        {
            if (p.GetComponent<NetworkObject>().InputAuthority == playerRef)
                return p;
        }
        return null;
    }

    // �ߺ� ���� ���� ID �̱�
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