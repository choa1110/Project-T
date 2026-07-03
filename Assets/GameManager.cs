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
    // ── 싱글톤 ──
    static GameManager _instance;
    public static GameManager Instance => _instance;

    // ── 네트워크 ──
    NetworkRunner _runner;

    // ── 플레이어 ──
    public NetworkPrefabRef playerPrefab;
    public List<Player> playerList = new List<Player>();

    // ── 라운드 (main) ──
    [Networked] public float RoundTimer { get; set; }
    [Networked] public int   CurrentRound { get; set; }   // [머지] HEAD 라운드 카운터 유지
    [Networked] public int   MaxRounds { get; set; }
    [Networked] public int   InitialLives { get; set; }
    [Networked] public float RoundDuration { get; set; }
    [Networked] public bool GameOver { get; set; }
    bool _isCardUIOpened = false;

    // ── 맵 (HEAD) ──
    [Header("Map")]
    [SerializeField] MapDB mapDB;
    NetworkObject _currentMapObject;
    MapInfo       _currentMapInfo;

    // ── 아이템 UI (HEAD) ──
    [Header("Item UI")]
    public ItemSlot[] itemSlots;

    // ── 랜덤 박스 (main) ──
    [Header("Random Box Settings")]
    public NetworkPrefabRef randomBoxPrefab;
    [SerializeField] float randomBoxSpawnInterval = 10f;
    [SerializeField] int maxRandomBoxes = 5;
    [SerializeField] Vector2Int randomBoxItemRange = new Vector2Int(0, 2);
    [SerializeField] Vector2 mapBoundsX = new Vector2(-15f, 15f); // 맵 드랍존 없을 때 폴백
    [SerializeField] Vector2 mapBoundsZ = new Vector2(-15f, 15f);
    [SerializeField] float spawnHeight = 1f;
    [Networked] TickTimer randomBoxTimer { get; set; }

    // ── 인풋 ──
    [SerializeField] InputActionAsset inputActions;
    InputAction move, jump, attack, sprint, guard, skill, useItem1, useItem2;

    // ════════════════════════════════════════════
    void Awake()
    {
        if (_instance == null) _instance = this;

        _runner = FindFirstObjectByType<NetworkRunner>();
        if (_runner != null)
        {
            _runner.AddCallbacks(this);
            Debug.Log("<color=green>GameManager OK</color>");
        }
        else Debug.LogError("<color=red>GameManager: NetworkRunner 없음!</color>");

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

    public override void Spawned()
    {
        if (!Object.HasStateAuthority) return;

        // [머지] main: 세션 설정 동기화
        if (Runner.SessionInfo.Properties.TryGetValue("Rounds", out var rounds)) MaxRounds = (int)rounds; else MaxRounds = 3;
        if (Runner.SessionInfo.Properties.TryGetValue("Lives",  out var lives))  InitialLives = (int)lives; else InitialLives = 3;
        if (Runner.SessionInfo.Properties.TryGetValue("Time",   out var time))   RoundDuration = (int)time; else RoundDuration = 120f;

        RoundTimer      = RoundDuration;
        CurrentRound    = 1;
        _isCardUIOpened = false;
        randomBoxTimer  = TickTimer.CreateFromSeconds(Runner, randomBoxSpawnInterval);

        RunSpawnMap(); // [머지] HEAD: 첫 맵 스폰
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;
        if (GameOver) return;

        if (RoundTimer > 0 && !_isCardUIOpened)
        {
            RoundTimer -= Runner.DeltaTime;
            if (RoundTimer <= 0)
            {
                RoundTimer = 0;
                _isCardUIOpened = true;
                EndRound();
            }
        }

        if (randomBoxTimer.Expired(Runner))
        {
            if (CanSpawnRandomBox()) SpawnRandomBox();
            randomBoxTimer = TickTimer.CreateFromSeconds(Runner, randomBoxSpawnInterval);
        }
    }

    // ════════════ 라운드 ════════════
    void EndRound()
    {
        if (!Object.HasStateAuthority) return;

        var ranked = playerList
            .OrderByDescending(p => p.IsDead ? -1 : (p.CurrentLives * 1000 + p.CurrentHP))
            .ToList();

        for (int i = 0; i < ranked.Count; i++)
        {
            int points = 0;
            switch (i) { case 0: points = 10; break; case 1: points = 8; break; case 2: points = 5; break; case 3: points = 3; break; }
            ranked[i].TotalScore += points;
            Debug.Log($"[Round End] {ranked[i].NickName} rank {i + 1}, +{points}");
        }

        // [머지] main의 CardUI 대신 HEAD의 버프 선택 페이즈 → 끝나면 BuffManager가 StartNextRound 호출
        BuffManager.Instance.StartBuffSelectionPhase(CurrentRound);
    }

    void DeclareWinner()
    {
        var winner = playerList.OrderByDescending(p => p.TotalScore).FirstOrDefault();
        string wname = winner != null ? winner.NickName.ToString() : "무승부";
        int wscore = winner != null ? winner.TotalScore : 0;
        GameOver = true;
        Debug.Log($"[Game Over] Winner: {wname} ({wscore}pts)");
        RPC_GameOver(wname, wscore);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_GameOver(string winnerName, int winnerScore)
    {
        GameOverUI.Instance?.Show(winnerName, winnerScore);
        Debug.Log($"[Game Over] 승자: {winnerName} ({winnerScore}점)");
    }

    // BuffManager.PhaseEnd 에서 호출됨
    public void StartNextRound()
    {
        if (!Object.HasStateAuthority) return;

        // [머지] 마지막 라운드면 승자 선언 (main의 MaxRounds 판정을 여기로 통합)
        if (CurrentRound >= MaxRounds) { DeclareWinner(); return; }

        CurrentRound++;
        RoundTimer      = RoundDuration;
        _isCardUIOpened = false;

        RunSpawnMap(); // [머지] HEAD: 라운드마다 새 맵

        foreach (var player in playerList)
        {
            player.RoundStart();
            RPC_ResetPlayerVisuals(player.Object);
            // [머지-fix] 새 맵 스폰포인트로 재배치 (낙사 방지)
            var ncc = player.GetComponent<NetworkCharacterController>();
            if (ncc != null) ncc.Teleport(CalcSpawnPos(player.Object.InputAuthority.PlayerId));
        }
        Debug.Log($"[GM] 라운드 {CurrentRound} 시작");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_ResetPlayerVisuals(NetworkObject playerObj)
    {
        if (playerObj == null) return;
        Player p = playerObj.GetComponent<Player>();
        if (p != null)
        {
            p.OnModelNumChanged();
            p.GetComponent<NetworkCharacterController>().enabled = true;
        }
    }

    public Player GetAlivePlayer(Player current)
    {
        var alive = playerList.Where(p => !p.IsDead).ToList();
        if (alive.Count == 0) return null;
        if (current == null || !alive.Contains(current)) return alive[0];
        int index = alive.IndexOf(current);
        return alive[(index + 1) % alive.Count];
    }

    // ════════════ 아이템 박스 ════════════
    bool CanSpawnRandomBox()
    {
        int count = 0;
        foreach (var obj in Runner.GetAllNetworkObjects())
            if (obj.GetComponent<ItemBox>() != null) count++;
        return count < maxRandomBoxes;
    }

    void SpawnRandomBox()
    {
        if (randomBoxPrefab == null) return;

        // [머지] HEAD: 맵 드랍존 우선, 없으면 main의 바운즈 폴백
        Vector3 pos = _currentMapInfo != null
            ? _currentMapInfo.GetRandomItemDropPosition()
            : new Vector3(UnityEngine.Random.Range(mapBoundsX.x, mapBoundsX.y), spawnHeight,
                          UnityEngine.Random.Range(mapBoundsZ.x, mapBoundsZ.y));

        NetworkObject box = Runner.Spawn(randomBoxPrefab, pos, Quaternion.identity);
        ItemBox itemBox = box.GetComponent<ItemBox>();
        if (itemBox != null) itemBox.SetItemRange(randomBoxItemRange.x, randomBoxItemRange.y);

        Debug.Log($"[GM] RandomBox @ {pos}");
    }

    // ════════════ 플레이어 등록/조회 ════════════
    public void RegisterPlayer(Player player)
    {
        if (!playerList.Contains(player)) playerList.Add(player);
    }

    public Player GetClosesetOpponent(Player user)
    {
        Player target = null;
        float minDist = float.MaxValue;
        foreach (var p in playerList)
        {
            if (p == user) continue;
            float dist = Vector3.Distance(user.transform.position, p.transform.position);
            if (dist < minDist) { minDist = dist; target = p; }
        }
        return target != null ? target : user;
    }

    public void WaitForRegister(Player mainChar) => StartCoroutine(OnRegisterDone(mainChar));

    IEnumerator OnRegisterDone(Player mainChar)
    {
        while (Runner.ActivePlayers.Count() > playerList.Count)
            yield return null;
        HUDManager.Instance.SetOpponentUI(mainChar);
    }

    // ════════════ 스폰 헬퍼 ════════════
    void RunSpawnMap()
    {
        if (!Object.HasStateAuthority) return;

        MapDB db = mapDB != null ? mapDB : MapDB.Instance;
        if (db == null) { Debug.LogWarning("[GM] MapDB 없음"); return; }

        MapData sel = db.GetRandomMap();
        if (sel == null) return;

        if (_currentMapObject != null) Runner.Despawn(_currentMapObject);

        _currentMapObject = Runner.Spawn(sel.mapPrefab, Vector3.zero, Quaternion.identity);
        _currentMapInfo   = _currentMapObject != null ? _currentMapObject.GetComponent<MapInfo>() : null;

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

    // [머지] main: 팀/모델 배정 + 셔플. 위치는 HEAD의 CalcSpawnPos(맵) 사용
    void SpawnAllCharacters(NetworkRunner runner)
    {
        if (!Object.HasStateAuthority) return;

        List<int> modelIndices = new List<int> { 0, 1, 2, 3 };
        for (int i = 0; i < modelIndices.Count; i++)
        {
            int r = UnityEngine.Random.Range(i, modelIndices.Count);
            (modelIndices[i], modelIndices[r]) = (modelIndices[r], modelIndices[i]);
        }

        int gameModeVal = 0;
        if (runner.SessionInfo != null && runner.SessionInfo.Properties.TryGetValue("GameMode", out var gmProp))
            gameModeVal = (int)gmProp;

        int spawnIndex = 0;
        foreach (var player in runner.ActivePlayers)
        {
            if (runner.GetPlayerObject(player) != null) { spawnIndex++; continue; }

            RoomPlayer roomPlayer = RoomPlayer.Players.Find(rp => rp.Object != null && rp.Object.InputAuthority == player);
            int assignedIndex = spawnIndex;
            if (roomPlayer != null)
            {
                int lobbyIndex = RoomPlayer.Players.IndexOf(roomPlayer);
                if (lobbyIndex != -1) assignedIndex = lobbyIndex;
            }

            int assignedModel = modelIndices[assignedIndex % modelIndices.Count];

            int assignedTeam;
            if (gameModeVal == 1) // 팀전
                assignedTeam = roomPlayer != null ? roomPlayer.Team : (assignedIndex < 2 ? 0 : 1);
            else
                assignedTeam = assignedIndex % 3;

            SpawnGameCharacter(runner, player, assignedModel, assignedTeam);
            spawnIndex++;
        }
    }

    void SpawnGameCharacter(NetworkRunner runner, PlayerRef player, int assignedModelNum = -1, int assignedTeam = 0)
    {
        if (!Object.HasStateAuthority) return;
        if (runner.GetPlayerObject(player) != null) return;

        Vector3 spawnPosition = CalcSpawnPos(player.PlayerId); // [머지] 맵 스폰포인트
        if (assignedModelNum == -1) assignedModelNum = UnityEngine.Random.Range(0, 4);

        NetworkObject playerChar = runner.Spawn(playerPrefab, spawnPosition, Quaternion.identity, player, (r, obj) =>
        {
            Player p = obj.GetComponent<Player>();
            if (p != null)
            {
                p.ModelNum = assignedModelNum;
                p.IsModelAssigned = true;
                p.team = assignedTeam;
            }
        });

        runner.SetPlayerObject(player, playerChar);
        Debug.Log($"{player} 스폰 완료 (모델 {assignedModelNum}, 팀 {assignedTeam})");
    }

    // ════════════ 인풋 ════════════
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

    // ════════════ INetworkRunnerCallbacks ════════════
    public void OnSceneLoadDone(NetworkRunner runner)
    {
        // [머지] 씬 로드 완료 시 팀/모델 배정으로 전원 스폰(main 방식)
        if (runner.IsServer) SpawnAllCharacters(runner);
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer) SpawnGameCharacter(runner, player); // 도중 합류자
    }

    public void OnObjectExitAOI(NetworkRunner r, NetworkObject o, PlayerRef p)                            { }
    public void OnObjectEnterAOI(NetworkRunner r, NetworkObject o, PlayerRef p)                           { }
    public void OnPlayerLeft(NetworkRunner r, PlayerRef p)                                                { }
    public void OnShutdown(NetworkRunner r, ShutdownReason s)                                             { }
    public void OnDisconnectedFromServer(NetworkRunner r, NetDisconnectReason n)                          { }
    public void OnConnectRequest(NetworkRunner r, NetworkRunnerCallbackArgs.ConnectRequest req, byte[] t) { }
    public void OnConnectFailed(NetworkRunner r, NetAddress a, NetConnectFailedReason n)                  { }
    public void OnUserSimulationMessage(NetworkRunner r, SimulationMessagePtr m)                          { }
    public void OnReliableDataReceived(NetworkRunner r, PlayerRef p, ReliableKey k, ArraySegment<byte> d) { }
    public void OnReliableDataProgress(NetworkRunner r, PlayerRef p, ReliableKey k, float f)              { }
    public void OnInputMissing(NetworkRunner r, PlayerRef p, NetworkInput i)                              { }
    public void OnConnectedToServer(NetworkRunner r)                                                       { }
    public void OnSessionListUpdated(NetworkRunner r, List<SessionInfo> s)                                { }
    public void OnCustomAuthenticationResponse(NetworkRunner r, Dictionary<string, object> d)             { }
    public void OnHostMigration(NetworkRunner r, HostMigrationToken t)                                    { }
    public void OnSceneLoadStart(NetworkRunner r)                                                          { }
}