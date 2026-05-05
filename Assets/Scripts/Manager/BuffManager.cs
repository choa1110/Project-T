using Fusion;
using UnityEngine;
using System.Collections.Generic;

public class BuffManager : NetworkBehaviour
{
    public static BuffManager Instance;

    [Networked] private int  _done  { get; set; }
    [Networked] private int  _total { get; set; }
    [Networked] public  bool IsSelectionPhase { get; set; }

    void Awake() { Instance = this; }

    public void StartBuffSelectionPhase(int rank)
    {
        if (!Object.HasStateAuthority) return;
        _done = 0; _total = 0; IsSelectionPhase = true;
        Debug.Log($"[BuffManager] 버프 선택 페이즈 시작 (rank {rank})");
        foreach (var pr in Runner.ActivePlayers)
        {
            _total++;
            RPC_OpenUI(pr, BuildOptions(3, rank));
        }
        if (_total == 0) PhaseEnd(true);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_OpenUI([RpcTarget] PlayerRef target, int[] opts)
    {
        if (Runner.LocalPlayer != target) return;
        GameManager.Instance?.OnDisableKeyInput();
        if (opts == null || opts.Length == 0) { SendSelectionToServer(-1); return; }
        BuffSelectionUI.Instance?.OpenSelection(opts);
    }

    public void SendSelectionToServer(int id) { RPC_Submit(id); }

    [Rpc(RpcSources.InputAuthority | RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_Submit(int id, RpcInfo info = default)
    {
        if (!IsSelectionPhase) return;
        if (id >= 0)
        {
            Player pl = Lookup(info.Source);
            if (pl != null)
            {
                Buff b = BuffDatabase.Instance?.GetBuffByID(id);
                if (b != null) { Debug.Log($"[BuffManager] {info.Source}: {b.buffName}"); pl.GetComponent<BuffSystem>().ApplyBuff(b); }
                else Debug.LogWarning($"[BuffManager] buffID={id} 없음");
            }
        }
        _done++;
        Debug.Log($"[BuffManager] {_done}/{_total} 완료");
        if (_done >= _total) PhaseEnd(false);
    }

    private void PhaseEnd(bool immediate)
    {
        IsSelectionPhase = false;
        Debug.Log($"[BuffManager] 페이즈 종료 (immediate={immediate}) -> 다음 라운드");
        RPC_BroadcastEnd(immediate);
        GameManager.Instance?.StartNextRound();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BroadcastEnd(bool wasImmediate)
    {
        GameManager.Instance?.OnEnableKeyInput();
        BuffSelectionUI.Instance?.ForceClose();
        Debug.Log($"[BuffManager] 클라이언트 정리 (immediate={wasImmediate})");
    }

    private Player Lookup(PlayerRef pr)
    {
        foreach (var p in FindObjectsOfType<Player>())
            if (p.GetComponent<NetworkObject>().InputAuthority == pr) return p;
        return null;
    }

    private int[] BuildOptions(int count, int rank)
    {
        var db = BuffDatabase.Instance;
        if (db == null || db.allBuffs == null || db.allBuffs.Count == 0)
        {
            Debug.LogWarning("[BuffManager] BuffDatabase 없음!");
            return new int[0];
        }
        var all = db.allBuffs;
        var valid = new List<int>();
        for (int i = 0; i < all.Count; i++)
            if (all[i] != null && all[i].rank == rank) valid.Add(i);
        if (valid.Count == 0)
        {
            Debug.LogWarning($"[BuffManager] rank {rank} 없음 -> 전체에서 선택");
            for (int i = 0; i < all.Count; i++)
                if (all[i] != null) valid.Add(i);
        }
        if (valid.Count == 0) return new int[0];
        var sel = new HashSet<int>(); int tries = valid.Count * 10;
        while (sel.Count < Mathf.Min(count, valid.Count) && tries-- > 0)
            sel.Add(valid[Random.Range(0, valid.Count)]);
        int[] res = new int[sel.Count]; sel.CopyTo(res); return res;
    }
}