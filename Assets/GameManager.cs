using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.InputSystem;

public struct NetworkInputData : INetworkInput
{
    public Vector2 move;
    public NetworkButtons buttons;
}

public enum InputButton { Jump, Attack, Sprint, Guard, Skill, UseItem1, UseItem2 }

public class GameManager : NetworkBehaviour, INetworkRunnerCallbacks
{
    // ── 싱글톤 ──────────────────────────────────────────────
    static GameManager _instance;
    public static GameManager Instance => _instance;

    // ── 네트워크 ─────────────────────────────────────────────
    NetworkRunner _runner;

    // ── 플레이어 ─────────────────────────────────────────────
    public NetworkPrefabRef playerPrefab;
    public List<Player> playerList = new List<Player>();

    // ── 라운드 ───────────────────────────────────────────────
    [Networked] public float RoundTimer   { get; set; }
    [Networked] public int   CurrentRound { get; set; }
    bool _isCardUIOpened;

    // ── 카드 UI ──────────────────────────────────────────────
    public CardUI cardUI;

    // ── 아이템 박스 스포너 ───────────────────────────────────
    [Header("Item Box Spawner")]
    public NetworkPrefabRef itemBoxPrefab;
    public float boxSpawnInterval = 8f;
    float _boxSpawnTimer;

    // ── 맵 ──────────────────────────────────────────────────
    [Header("Map")]
    [SerializeField] MapDB mapDB;
    NetworkObject _currentMapObject;
    MapInfo       _currentMapInfo;

    // ── 아이템 슬롯 ──────────────────────────────────────────
    [Header("Item UI")]
    public ItemSlot[] itemSlots;

    // ── 인풋 ─────────────────────────────────────────────────
    [SerializeField] InputActionAsset inputActions;
    InputAction move, jump, attack, sprint, guard, skill, useItem1, useItem2;

    // ════════════════════════════════════════════════════════
    // Unity 생명주기
    // ════════════════════════════════════════════════════════
    void Awake()
    {
        if (_instance == null) _instance = this;

        _runner = FindFirstObjectByType<NetworkRunner>();
        if (_runner != null)
        {
            _runner.AddCallbacks(this);
            Debug.Log("<color=green>GameManager OK</color>");
        }
        else
        {
            Debug.LogError("<color=red>GameManager: NetworkRunner 없음!</color>");
        }

        var m = inputActions.FindActionMap("Player");
        move     = m.FindAction("Move");
        jump     = m.FindAction("Jump");
        attack   = m.FindAction("Attack");
        sprint   = m.FindAction("Sprint");
        guard    = m.FindAction("Guard");
        skill    = m.FindAction("Skill");
        useItem1 = m.FindAction("UseItem1");
        useItem2 = m.FindAction("UseItem2");

        OnEnableKeyInput();
    }

    // ════════════════════════════════════════════════════════
    // Fusion 생명주기
    // ════════════════════════════════════════════════════════
    public override void Spawned()
    {
        if (!Object.HasStateAuthority) return;

        RoundTimer      = 180f;
        CurrentRound    = 1;
        _isCardUIOpened = false;
        _boxSpawnTimer  = boxSpawnInterval;

        RunSpawnMap();
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        // 라운드 타이머
        if (RoundTimer > 0 && !_isCardUIOpened)
        {
            RoundTimer -= Runner.DeltaTime;
            if (RoundTimer <= 0)
            {
                RoundTimer      = 0;
                _isCardUIOpened = true;
                BuffManager.Instance.StartBuffSelectionPhase(CurrentRound);
            }
        }

        // 아이템 박스 스폰
        _boxSpawnTimer -= Runner.DeltaTime;
        if (_boxSpawnTimer <= 0)
        {
            _boxSpawnTimer = boxSpawnInterval;
            RunSpawnBox();
        }
    }

    // ════════════════════════════════════════════════════════
    // 라운드 관리
    // ════════════════════════════════════════════════════════
    public void StartNextRound()
    {
        if (!Object.HasStateAuthority) return;

        CurrentRound++;
        RoundTimer      = 180f;
        _isCardUIOpened = false;

        RunSpawnMap();

        foreach (var pr in Runner.ActivePlayers)
        {
            var obj = Runner.GetPlayerObject(pr);
            if (obj == null) continue;
            obj.transform.position = CalcSpawnPos(pr.PlayerId);
            obj.GetComponent<Player>()?.RoundStart();
        }

        Debug.Log($"[GM] 라운드 {CurrentRound} 시작");
    }

    // ════════════════════════════════════════════════════════
    // 플레이어 등록 / 조회
    // ════════════════════════════════════════════════════════
    public void RegisterPlayer(Player player)
    {
        if (!playerList.Contains(player))
            playerList.Add(player);
    }

    public Player GetClosestOpponent(Player user)
    {
        Player target  = null;
        float  minDist = float.MaxValue;

        foreach (var p in playerList)
        {
            if (p == user) continue;
            float dist = Vector3.Distance(user.transform.position, p.transform.position);
            if (dist < minDist) { minDist = dist; target = p; }
        }

        return target != null ? target : user;
    }

    public void WaitForRegister(Player mainChar)
    {
        StartCoroutine(OnRegisterDone(mainChar));
    }

    IEnumerator OnRegisterDone(Player mainChar)
    {
        while (Runner.ActivePlayers.Count() > playerList.Count)
            yield return null;

        HUDManager.Instance.SetOpponentUI(mainChar);
    }

    // ════════════════════════════════════════════════════════
    // RPC — 카드 UI
    // ════════════════════════════════════════════════════════
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ShowCardUI(int round)
    {
        if (cardUI != null)
            cardUI.OpenCardSelection(round);
        else
            Debug.LogError("GameManager에 CardUI가 연결되어 있지 않습니다!");
    }

    // ════════════════════════════════════════════════════════
    // 스폰 헬퍼
    // ════════════════════════════════════════════════════════
    void RunSpawnBox()
    {
        Vector3 pos = _currentMapInfo != null
            ? _currentMapInfo.GetRandomItemDropPosition()
            : new Vector3(UnityEngine.Random.Range(-25f, 25f), 1f,
                          UnityEngine.Random.Range(-25f, 25f));
        Runner.Spawn(itemBoxPrefab, pos, Quaternion.identity);
    }

    void RunSpawnMap()
    {
        if (!Object.HasStateAuthority) return;

        MapDB db = mapDB != null ? mapDB : MapDB.Instance;
        if (db == null) { Debug.LogWarning("[GM] MapDB 없음"); return; }

        MapData sel = db.GetRandomMap();
        if (sel == null) return;

        if (_currentMapObject != null) Runner.Despawn(_currentMapObject);

        _currentMapObject = Runner.Spawn(sel.mapPrefab, Vector3.zero, Quaternion.identity);
        _currentMapInfo   = _currentMapObject != null
            ? _currentMapObject.GetComponent<MapInfo>()
            : null;

        Debug.Log($"[GM] 맵: {sel.mapName}");
    }

    Vector3 CalcSpawnPos(int playerId)
    {
        if (_currentMapInfo != null)
        {
            var t = _currentMapInfo.GetPlayerSpawnPoint(playerId);
            if (t != null) return t.position;
        }
        return new Vector3(playerId * 2f, 3f, 0f);
    }

    void RunSpawnPlayer(NetworkRunner runner, PlayerRef player)
    {
        if (runner.GetPlayerObject(player) != null) return;
        runner.Spawn(playerPrefab, CalcSpawnPos(player.PlayerId), Quaternion.identity, player);
        Debug.Log($"[GM] {player.PlayerId}번 플레이어 스폰 완료");
    }

    // ════════════════════════════════════════════════════════
    // 인풋
    // ════════════════════════════════════════════════════════
    public void OnEnableKeyInput()  => inputActions.Enable();
    public void OnDisableKeyInput() => inputActions.Disable();

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var d = new NetworkInputData { move = move.ReadValue<Vector2>() };
        d.buttons.Set(InputButton.Jump,     jump.IsPressed());
        d.buttons.Set(InputButton.Attack,   attack.IsPressed());
        d.buttons.Set(InputButton.Sprint,   sprint.IsPressed());
        d.buttons.Set(InputButton.Guard,    guard.IsPressed());
        d.buttons.Set(InputButton.Skill,    skill.IsPressed());
        d.buttons.Set(InputButton.UseItem1, useItem1.IsPressed());
        d.buttons.Set(InputButton.UseItem2, useItem2.IsPressed());
        input.Set(d);
    }

    // ════════════════════════════════════════════════════════
    // INetworkRunnerCallbacks
    // ════════════════════════════════════════════════════════
    public void OnSceneLoadDone(NetworkRunner runner)
    {
        if (runner.IsServer)
            foreach (var p in runner.ActivePlayers)
                RunSpawnPlayer(runner, p);
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
            RunSpawnPlayer(runner, player);
    }

    public void OnObjectExitAOI(NetworkRunner r, NetworkObject o, PlayerRef p)                          { }
    public void OnObjectEnterAOI(NetworkRunner r, NetworkObject o, PlayerRef p)                         { }
    public void OnPlayerLeft(NetworkRunner r, PlayerRef p)                                              { }
    public void OnShutdown(NetworkRunner r, ShutdownReason s)                                           { }
    public void OnDisconnectedFromServer(NetworkRunner r, NetDisconnectReason n)                        { }
    public void OnConnectRequest(NetworkRunner r, NetworkRunnerCallbackArgs.ConnectRequest req, byte[] t) { }
    public void OnConnectFailed(NetworkRunner r, NetAddress a, NetConnectFailedReason n)                { }
    public void OnUserSimulationMessage(NetworkRunner r, SimulationMessagePtr m)                        { }
    public void OnReliableDataReceived(NetworkRunner r, PlayerRef p, ReliableKey k, ArraySegment<byte> d) { }
    public void OnReliableDataProgress(NetworkRunner r, PlayerRef p, ReliableKey k, float f)            { }
    public void OnInputMissing(NetworkRunner r, PlayerRef p, NetworkInput i)                            { }
    public void OnConnectedToServer(NetworkRunner r)                                                     { }
    public void OnSessionListUpdated(NetworkRunner r, List<SessionInfo> s)                              { }
    public void OnCustomAuthenticationResponse(NetworkRunner r, Dictionary<string, object> d)           { }
    public void OnHostMigration(NetworkRunner r, HostMigrationToken t)                                  { }
    public void OnSceneLoadStart(NetworkRunner r)                                                        { }
}