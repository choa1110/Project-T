using Fusion;
using Fusion.Sockets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public struct NetworkInputData : INetworkInput
{
    public Vector2 move;
    public NetworkButtons buttons;
}

public enum InputButton
{
    Jump,
    Attack,
    Sprint,
    Guard,
    Skill,
    UseItem1,
    UseItem2
}

public class GameManager : NetworkBehaviour, INetworkRunnerCallbacks
{
    static GameManager _instance;
    public static GameManager Instance { get => _instance; }

    private NetworkRunner _netRunner;

    public NetworkPrefabRef playerPrefab;

    public List<Player> playerList = new List<Player>();

    [SerializeField] List<MapInfo> mapList = new List<MapInfo>();
    MapInfo curMap;

    [Networked] public float RoundTimer { get; set; }
    [Networked] public int MaxRounds { get; set; }
    [Networked] public int InitialLives { get; set; }
    [Networked] public float RoundDuration { get; set; }
    [Networked] TickTimer transitionTimer { get; set; }
    [Networked] bool _isTransitioning { get; set; }
    public bool IsTransitioning => _isTransitioning;
    [Networked] public int TransitionPhase { get; set; }

    public CardUI cardUI;
    bool _isCardUIOpened = false;

    [Header("Random Box Settings")]
    [SerializeField] NetworkObject randomBoxPrefab;
    [SerializeField] float randomBoxSpawnInterval = 7.5f;

    [Networked] TickTimer randomBoxTimer { get; set; }

    List<GameObject> ResetObjects = new List<GameObject>();

    [SerializeField] InputActionAsset inputActions;

    InputAction move;
    InputAction jump;
    InputAction attack;
    InputAction sprint;
    InputAction guard;
    InputAction skill;
    InputAction useItem1;
    InputAction useItem2;

    void Awake()
    {
        if (_instance == null)
            _instance = this;

        _netRunner = FindFirstObjectByType<NetworkRunner>();

        if (_netRunner != null)
        {
            _netRunner.AddCallbacks(this);
            Debug.Log("<color=green>GameManager: Runner 찾음! 콜백 등록 완료!</color>");
        }
        else
        {
            Debug.LogError("<color=red>GameManager: 심각한 문제! NetworkRunner를 찾을 수 없습니다!</color>");
        }

        var map = inputActions.FindActionMap("Player");

        move = map.FindAction("Move");
        jump = map.FindAction("Jump");
        attack = map.FindAction("Attack");
        sprint = map.FindAction("Sprint");
        guard = map.FindAction("Guard");
        skill = map.FindAction("Skill");
        useItem1 = map.FindAction("UseItem1");
        useItem2 = map.FindAction("UseItem2");

        OnEnableKeyInput();
    }

    void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
        if (_netRunner != null)
            _netRunner.RemoveCallbacks(this);
    }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            // Sync settings from SessionProperties
            if (Runner.SessionInfo.Properties.TryGetValue("Rounds", out var rounds)) MaxRounds = (int)rounds;
            else MaxRounds = 3;

            if (Runner.SessionInfo.Properties.TryGetValue("Lives", out var lives)) InitialLives = (int)lives;
            else InitialLives = 3;

            if (Runner.SessionInfo.Properties.TryGetValue("Time", out var time)) RoundDuration = (int)time;
            else RoundDuration = 120f;

            RoundTimer = RoundDuration;
            _isCardUIOpened = false;
            _isTransitioning = false;
            randomBoxTimer = TickTimer.CreateFromSeconds(Runner, randomBoxSpawnInterval);

            curMap = mapList[UnityEngine.Random.Range(0, mapList.Count)];
            curMap.Rpc_BroadcastToggleMap(true);
        }
    }

    [Networked, OnChangedRender(nameof(OnCurrentRoundChanged))] public int CurrentRound { get; set; } = 1;

    void OnCurrentRoundChanged()
    {
        if (ScoreDisplayUI.Instance != null)
        {
            ScoreDisplayUI.Instance.HideAll();
        }
        if (cardUI != null && cardUI.cardPanel != null)
        {
            cardUI.cardPanel.SetActive(false);
        }

        var timerDisplay = FindFirstObjectByType<TimerDisplay>();
        if (timerDisplay != null)
        {
            timerDisplay.UpdateDisplay(RoundDuration);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority)
        {
            // Clean up player list from null/invalid player references
            playerList.RemoveAll(p => p == null || p.Object == null || !p.Object.IsValid);

            if (_isTransitioning)
            {
                if (TransitionPhase == 2 && playerList.Count > 0 && playerList.All(p => p.HasSelectedCard))
                {
                    // Transition to Phase 3 (Post-selection wait) immediately: 3 seconds
                    TransitionPhase = 3;
                    transitionTimer = TickTimer.CreateFromSeconds(Runner, 3f);
                }
                else if (transitionTimer.Expired(Runner))
                {
                    if (TransitionPhase == 1)
                    {
                        // Transition to Phase 2 (Card Selection): 10 seconds
                        TransitionPhase = 2;
                        transitionTimer = TickTimer.CreateFromSeconds(Runner, 10f);
                        foreach (var p in playerList)
                        {
                            p.HasSelectedCard = false;
                        }
                        RPC_ShowCardUI(CurrentRound);
                    }
                    else if (TransitionPhase == 2)
                    {
                        // Transition to Phase 3 (Post-selection wait): 3 seconds
                        TransitionPhase = 3;
                        transitionTimer = TickTimer.CreateFromSeconds(Runner, 3f);
                    }
                    else if (TransitionPhase == 3)
                    {
                        _isTransitioning = false;
                        TransitionPhase = 0;
                        RPC_HideCardUI();
                        if (CurrentRound < MaxRounds)
                        {
                            BackGroundMusic.Instance.Rpc_BroadcastRaiseSound();
                            StartNextRound();
                        }
                        else
                        {
                            DeclareWinner();
                        }
                    }
                }
                return;
            }

            if (RoundTimer > 0 && !_isCardUIOpened)
            {
                RoundTimer -= Runner.DeltaTime;

                if (RoundTimer <= 0 || CheckRoundEndConditions())
                {
                    RoundTimer = 0;
                    _isCardUIOpened = true;

                    EndRound();
                }
            }

            // Random Box Spawning Logic
            if (randomBoxTimer.Expired(Runner))
            {
                SpawnRandomBox();
                
                randomBoxTimer = TickTimer.CreateFromSeconds(Runner, randomBoxSpawnInterval);
            }
        }
    }

    bool CheckRoundEndConditions()
    {
        if (playerList.Count == 0) return false;

        // Ensure all active players have registered in the player list first
        int activeCount = Runner.ActivePlayers.Count();
        int registeredCount = playerList.Count(p => p != null && p.Object != null && p.Object.IsValid);
        if (registeredCount < activeCount) return false;

        // Check if GameMode is Team Match (1 = Team Match)
        bool isTeamMatch = false;
        if (Runner != null && Runner.SessionInfo != null && 
            Runner.SessionInfo.Properties.TryGetValue("GameMode", out var gmProp))
        {
            isTeamMatch = (int)gmProp == 1;
        }

        if (isTeamMatch)
        {
            // Count alive players per team
            int redAlive = playerList.Count(p => p != null && p.team == 0 && p.CurrentLives > 0);
            int blueAlive = playerList.Count(p => p != null && p.team == 1 && p.CurrentLives > 0);

            int redTotal = playerList.Count(p => p != null && p.team == 0);
            int blueTotal = playerList.Count(p => p != null && p.team == 1);

            // If a team has players but all are out of lives, end the round early
            if (redTotal > 0 && redAlive == 0) return true;
            if (blueTotal > 0 && blueAlive == 0) return true;
        }
        else
        {
            // Solo Mode
            int aliveCount = playerList.Count(p => p != null && p.CurrentLives > 0);
            if (playerList.Count > 1)
            {
                if (aliveCount <= 1) return true;
            }
            else
            {
                // Single player testing mode
                if (aliveCount == 0) return true;
            }
        }

        return false;
    }

    void EndRound()
    {
        if (!Object.HasStateAuthority) return;

        // Calculate rankings for the round
        var rankedPlayers = playerList
            .OrderByDescending(p => p.IsDead ? -1 : (p.CurrentLives * 1000 + p.CurrentHP))
            .ToList();

        for (int i = 0; i < rankedPlayers.Count; i++)
        {
            int points = 0;
            switch (i)
            {
                case 0: points = 10; break;
                case 1: points = 8; break;
                case 2: points = 5; break;
                case 3: points = 3; break;
            }
            rankedPlayers[i].TotalScore += points;
            Debug.Log($"[Round End] {rankedPlayers[i].NickName} ranked {i + 1} and got {points} points.");
        }

        BackGroundMusic.Instance.Rpc_BroadcastDimSound();
        if (CurrentRound < MaxRounds)
        {
            // Show Interim Score UI
            RPC_ShowInterimScoreUI(CurrentRound);
            
            // Start phase 1 transition timer (5 seconds for interim score display)
            transitionTimer = TickTimer.CreateFromSeconds(Runner, 5f);
            _isTransitioning = true;
            TransitionPhase = 1;
        }
        else
        {
            // Final round ended! Show final rankings immediately.
            DeclareWinner();
        }
    }

    void DeclareWinner()
    {
        var winner = playerList.OrderByDescending(p => p.TotalScore).FirstOrDefault();
        if (winner != null)
        {
            Debug.Log($"[Game Over] Winner is {winner.NickName} with {winner.TotalScore} points!");
        }
        RPC_ShowFinalRankingsUI();
    }

    public void StartNextRound()
    {
        if (!Object.HasStateAuthority) return;

        CurrentRound++;
        RoundTimer = RoundDuration;
        _isCardUIOpened = false;

        MapInfo tmp = mapList[UnityEngine.Random.Range(0, mapList.Count)];

        if (tmp != curMap)
        {
            DisposeObjects();
            curMap.Rpc_BroadcastToggleMap(false);
            tmp.Rpc_BroadcastToggleMap(true);
            curMap = tmp;
        }
        else
            curMap.ShufflePoints();

        for (int i = 0; i < playerList.Count; i++)
        {
            Transform transLo = curMap.GetPlayerSpawnPoint(i);
            playerList[i].Rpc_BroadcastSetSpawn(transLo.position, transLo.rotation);

            playerList[i].Rpc_BroadcastRoundStart();
            playerList[i].HasSelectedCard = false;
            //RPC_ResetPlayerVisuals(playerList[i].Object);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_ResetPlayerVisuals(NetworkObject playerObj)
    {
        if (playerObj == null) return;
        Player p = playerObj.GetComponent<Player>();
        if (p != null)
        {
            p.OnModelNumChanged(); // This re-enables the current model
            p.GetComponent<NetworkCharacterController>().enabled = true;

            // Re-enable skinned mesh renderer if it was disabled (e.g. by DeathZone)
            if (p.modelList != null && p.ModelNum >= 0 && p.ModelNum < p.modelList.Count)
            {
                var info = p.modelList[p.ModelNum].GetComponent<CharacterInfo>();
                if (info != null && info.mesh != null)
                {
                    info.mesh.enabled = true;
                }
            }

            // Reset camera target for the local player
            if (p.Object.HasInputAuthority && p.POV != null)
            {
                p.POV.ToggleSoft();
                p.POV.target = p;
            }

            // Reset HP fill bars instantly to full for the new round
            if (p.Object.HasInputAuthority)
            {
                if (HUDManager.Instance != null && HUDManager.Instance.hpBar != null)
                {
                    HUDManager.Instance.hpBar.SetFillBarInstant(1.0f);
                }
            }
            else
            {
                if (p.linkedOpponentData != null && p.linkedOpponentData.fillBar != null)
                {
                    p.linkedOpponentData.fillBar.SetFillBarInstant(1.0f);
                }
            }
        }
    }

    public Player GetAlivePlayer(Player current)
    {
        var alivePlayers = playerList.Where(p => !p.IsDead).ToList();
        if (alivePlayers.Count == 0) return null;

        if (current == null || !alivePlayers.Contains(current))
            return alivePlayers[0];

        int index = alivePlayers.IndexOf(current);
        return alivePlayers[(index + 1) % alivePlayers.Count];
    }

    public void SetDisposable(GameObject go)
    {
        ResetObjects.Add(go);
    }

    void DisposeObjects()
    {
        foreach (GameObject go in ResetObjects)
            if (go != null) Destroy(go);

        ResetObjects.Clear();
    }

    //bool CanSpawnRandomBox()
    //{
    //    int count = 0;
    //    foreach (var obj in Runner.GetAllNetworkObjects())
    //    {
    //        if (obj.GetComponent<RandomBox>() != null) count++;
    //    }
    //    return count < maxRandomBoxes;
    //}

    void SpawnRandomBox(int itemRangeX = 0, int itemRangeY = 4)
    {
        if (randomBoxPrefab == null) return;

        ItemPoint tmp = curMap.GetRandomItemDropPosition();

        if (tmp == null) return;

        //float x = UnityEngine.Random.Range(mapBoundsX.x, mapBoundsX.y);
        //float z = UnityEngine.Random.Range(mapBoundsZ.x, mapBoundsZ.y);
        //Vector3 pos = new Vector3(x, spawnHeight, z);

        Runner.Spawn(randomBoxPrefab, tmp.pos, Quaternion.identity, null, (runner, obj) => {
            if (obj.TryGetBehaviour<RandomBox>(out var boxBehaviour))
            {
                boxBehaviour.SetSpawnPoint(tmp);
                boxBehaviour.SetItemRange(itemRangeX, itemRangeY);
                SetDisposable(obj.gameObject);
            }
        });

        //NetworkObject box = Runner.Spawn(randomBoxPrefab, tmp.pos, Quaternion.identity);
        //RandomBox itemBox = box.GetComponent<RandomBox>();
        //if (itemBox != null)
        //{
        //    itemBox.SetItemRange(randomBoxItemRange.x, randomBoxItemRange.y);
        //}
        
        Debug.Log($"[GameManager] Spawned RandomBox at {tmp}");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ShowCardUI(int rank)
    {
        if(cardUI != null)
        {
            cardUI.OpenCardSelection(rank);
            Debug.Log("카드 선택 창 열림");
        }
        else
        {
            Debug.LogError("GameManager에 CardUI가 연결되어 있지 않습니다!");
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ShowInterimScoreUI(int roundNum)
    {
        if (ScoreDisplayUI.Instance != null)
        {
            ScoreDisplayUI.Instance.ShowInterimScores(roundNum);
        }
        else
        {
            Debug.LogError("GameManager: ScoreDisplayUI.Instance is null! Showing CardUI immediately.");
            if (cardUI != null)
            {
                cardUI.OpenCardSelection(roundNum);
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ShowFinalRankingsUI()
    {
        if (ScoreDisplayUI.Instance != null)
        {
            ScoreDisplayUI.Instance.ShowFinalRankings();
        }
        else
        {
            Debug.LogError("GameManager: ScoreDisplayUI.Instance is null!");
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_HideCardUI()
    {
        if (cardUI != null && cardUI.cardPanel != null)
        {
            cardUI.cardPanel.SetActive(false);
        }
    }

    public float GetCardSelectionRemainingTime()
    {
        if (Object == null || !Object.IsValid || !_isTransitioning || TransitionPhase != 2)
            return 0f;

        float? remaining = transitionTimer.RemainingTime(Runner);
        return remaining.HasValue ? Mathf.Max(0f, remaining.Value) : 0f;
    }

    public void RegisterPlayer(Player reg)
    {
        playerList.Add(reg);
    }

    public Player GetClosesetOpponent(Player user)
    {
        Player target = null;
        float minDis = float.PositiveInfinity;

        foreach (Player reg in playerList)
        {
            if (user != reg && user.team != reg.team && !reg.IsDead)
            {
                float dis = Vector3.Distance(user.transform.position, reg.transform.position);

                if (dis < minDis)
                {
                    minDis = dis;
                    target = reg;
                }
            }
        }

        if (target == null)
            return user;
        else
            return target;
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        if (runner.IsServer)
        {
            // Shuffle model indices [0, 1, 2, 3] to assign unique models to each player
            List<int> modelIndices = new List<int> { 0, 1, 2, 3 };
            for (int i = 0; i < modelIndices.Count; i++)
            {
                int temp = modelIndices[i];
                int randomIndex = UnityEngine.Random.Range(i, modelIndices.Count);
                modelIndices[i] = modelIndices[randomIndex];
                modelIndices[randomIndex] = temp;
            }

            int gameModeVal = 0;
            if (runner.SessionInfo != null && runner.SessionInfo.Properties.TryGetValue("GameMode", out var gmProp))
            {
                gameModeVal = (int)gmProp;
            }

            int spawnIndex = 0;
            foreach (var player in runner.ActivePlayers)
            {
                // Find this player's RoomPlayer from the lobby list to map their chosen team and index
                RoomPlayer roomPlayer = RoomPlayer.Players.Find(rp => rp.Object != null && rp.Object.InputAuthority == player);
                
                int assignedIndex = spawnIndex;
                if (roomPlayer != null)
                {
                    int lobbyIndex = RoomPlayer.Players.IndexOf(roomPlayer);
                    if (lobbyIndex != -1)
                    {
                        assignedIndex = lobbyIndex;
                    }
                }

                int assignedModel = modelIndices[assignedIndex % modelIndices.Count];
                
                // Determine team
                int assignedTeam = 0;
                if (gameModeVal == 1) // Team Match (2 vs 2)
                {
                    if (roomPlayer != null)
                    {
                        assignedTeam = roomPlayer.Team; // Directly use their chosen team from RoomPlayer
                    }
                    else
                    {
                        assignedTeam = (assignedIndex < 2) ? 0 : 1; // Fallback
                    }
                }
                else // Solo mode
                {
                    assignedTeam = assignedIndex % 4; // Cycle colors: 0 (Blue), 1 (Red), 2 (Green)
                }

                SpawnGameCharacter(runner, player, assignedModel, assignedTeam);
                spawnIndex++;
            }

            BackGroundMusic.Instance.Rpc_BroadcastBattleOn();
        }
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

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        //if (runner.IsServer)
        //{
        //    SpawnGameCharacter(runner, player);
        //}
    }

    void SpawnGameCharacter(NetworkRunner runner, PlayerRef player, int assignedModelNum = -1, int assignedTeam = 0)
    {
        NetworkObject existingPlayerObj = runner.GetPlayerObject(player);

        if (existingPlayerObj != null && existingPlayerObj.IsValid)
             return;

        // 2. 위치 계산
        Transform spawnTransform = curMap.GetPlayerSpawnPoint(player.PlayerId);

        //int index = player.PlayerId;
        //float xPos = index * 2f;
        //Vector3 spawnPosition = new Vector3(xPos, 3f, 0);
        
        // 3. 스폰 실행 (여기서 에러가 나는 건 코드가 아니라 playerPrefab 변수에 든 내용물 때문임)
        Debug.Log(this.name + " : " + playerPrefab);

        if (assignedModelNum == -1)
        {
            assignedModelNum = UnityEngine.Random.Range(0, 4);
        }

        NetworkObject playerChar = runner.Spawn(playerPrefab, spawnTransform.position, spawnTransform.rotation, player, (runner, obj) => {
            if (obj.TryGetBehaviour<Player>(out Player p))
            {
                p.ModelNum = assignedModelNum;
                p.IsModelAssigned = true;
                p.team = assignedTeam; // Assign networked team property

                p.Rpc_BroadcastSetSpawn(spawnTransform.position, spawnTransform.rotation);
            }
        });

        runner.SetPlayerObject(player, playerChar);
        Debug.Log($"{player}번 플레이어 스폰 완료 (위치: {spawnTransform}) (모델: {assignedModelNum}) (팀: {assignedTeam})");
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.LogWarning($"[Network] Shutdown 발생: {shutdownReason}");

        // Runner 객체 정리
        if (runner != null)
        {
            Destroy(runner.gameObject);
        }

        // 연결 끊김 원인에 따른 예외 처리 (선택)
        switch (shutdownReason)
        {
            case ShutdownReason.HostMigration:
                // 호스트 마이그레이션을 사용할 경우 예외 처리
                return;
            case ShutdownReason.Ok:
                // 정상적으로 나간 경우
                break;
            default:
                // 비정상적 끊김 (HostDied, ConnectionTimeout 등)
                Debug.Log("호스트와의 연결이 끊어졌습니다. 로비로 이동합니다.");
                break;
        }

        // 안전하게 로비 씬으로 이동
        SceneManager.LoadScene(0);
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        NetworkInputData data = new NetworkInputData();

        data.move = move.ReadValue<Vector2>();

        data.buttons.Set(InputButton.Jump, jump.IsPressed());
        data.buttons.Set(InputButton.Attack, attack.IsPressed());
        data.buttons.Set(InputButton.Sprint, sprint.IsPressed());
        data.buttons.Set(InputButton.Guard, guard.IsPressed());
        data.buttons.Set(InputButton.Skill, skill.IsPressed());
        data.buttons.Set(InputButton.UseItem1, useItem1.IsPressed());
        data.buttons.Set(InputButton.UseItem2, useItem2.IsPressed());

        input.Set(data);
    }

    public void OnEnableKeyInput()
    {
        inputActions.Enable();
    }

    public void OnDisableKeyInput()
    {
        inputActions.Disable();
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
    }
}
