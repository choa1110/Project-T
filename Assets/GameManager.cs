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

    [Networked] public float RoundTimer { get; set; }
    [Networked] public int MaxRounds { get; set; }
    [Networked] public int InitialLives { get; set; }
    [Networked] public float RoundDuration { get; set; }
    [Networked] TickTimer transitionTimer { get; set; }
    [Networked] bool _isTransitioning { get; set; }

    public CardUI cardUI;
    bool _isCardUIOpened = false;

    [Header("Random Box Settings")]
    public NetworkPrefabRef randomBoxPrefab;
    [SerializeField] float randomBoxSpawnInterval = 10f;
    [SerializeField] Vector2 mapBoundsX = new Vector2(-15f, 15f);
    [SerializeField] Vector2 mapBoundsZ = new Vector2(-15f, 15f);
    [SerializeField] float spawnHeight = 1f;
    [SerializeField] int maxRandomBoxes = 5;
    [SerializeField] Vector2Int randomBoxItemRange = new Vector2Int(0, 2);

    [Networked] TickTimer randomBoxTimer { get; set; }

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
        }
    }

    [Networked] public int CurrentRound { get; set; } = 1 ;

    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority)
        {
            if (_isTransitioning)
            {
                if (transitionTimer.Expired(Runner))
                {
                    _isTransitioning = false;
                    if (CurrentRound < MaxRounds)
                    {
                        StartNextRound();
                    }
                    else
                    {
                        DeclareWinner();
                    }
                }
                return;
            }

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

            // Random Box Spawning Logic
            if (randomBoxTimer.Expired(Runner))
            {
                if (CanSpawnRandomBox())
                {
                    SpawnRandomBox();
                }
                randomBoxTimer = TickTimer.CreateFromSeconds(Runner, randomBoxSpawnInterval);
            }
        }
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

        // Show Card UI for upgrades
        RPC_ShowCardUI(CurrentRound);
        
        // Start transition timer (e.g., 15 seconds to select cards)
        transitionTimer = TickTimer.CreateFromSeconds(Runner, 15f);
        _isTransitioning = true;
    }

    void DeclareWinner()
    {
        var winner = playerList.OrderByDescending(p => p.TotalScore).FirstOrDefault();
        if (winner != null)
        {
            Debug.Log($"[Game Over] Winner is {winner.NickName} with {winner.TotalScore} points!");
        }
    }

    public void StartNextRound()
    {
        if (!Object.HasStateAuthority) return;

        CurrentRound++;
        RoundTimer = RoundDuration;
        _isCardUIOpened = false;

        foreach (var player in playerList)
        {
            player.RoundStart();
            RPC_ResetPlayerVisuals(player.Object);
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

    bool CanSpawnRandomBox()
    {
        int count = 0;
        foreach (var obj in Runner.GetAllNetworkObjects())
        {
            if (obj.GetComponent<ItemBox>() != null) count++;
        }
        return count < maxRandomBoxes;
    }

    void SpawnRandomBox()
    {
        if (randomBoxPrefab == null) return;

        float x = UnityEngine.Random.Range(mapBoundsX.x, mapBoundsX.y);
        float z = UnityEngine.Random.Range(mapBoundsZ.x, mapBoundsZ.y);
        Vector3 pos = new Vector3(x, spawnHeight, z);

        NetworkObject box = Runner.Spawn(randomBoxPrefab, pos, Quaternion.identity);
        ItemBox itemBox = box.GetComponent<ItemBox>();
        if (itemBox != null)
        {
            itemBox.SetItemRange(randomBoxItemRange.x, randomBoxItemRange.y);
        }
        
        Debug.Log($"[GameManager] Spawned RandomBox at {pos}");
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
            if (user != reg) //reg.team != mainPlayer.team
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
                    assignedTeam = assignedIndex % 3; // Cycle colors: 0 (Blue), 1 (Red), 2 (Green)
                }

                SpawnGameCharacter(runner, player, assignedModel, assignedTeam);
                spawnIndex++;
            }
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
        if (runner.GetPlayerObject(player) != null)
        {
             return;
        }

        // 2. 위치 계산
        int index = player.PlayerId;
        float xPos = index * 2f;
        Vector3 spawnPosition = new Vector3(xPos, 3f, 0);
        
        // 3. 스폰 실행 (여기서 에러가 나는 건 코드가 아니라 playerPrefab 변수에 든 내용물 때문임)
        Debug.Log(this.name + " : " + playerPrefab);

        if (assignedModelNum == -1)
        {
            assignedModelNum = UnityEngine.Random.Range(0, 4);
        }

        NetworkObject playerChar = runner.Spawn(playerPrefab, spawnPosition, Quaternion.identity, player, (runner, obj) => {
            Player p = obj.GetComponent<Player>();
            if (p != null)
            {
                p.ModelNum = assignedModelNum;
                p.IsModelAssigned = true;
                p.team = assignedTeam; // Assign networked team property
            }
        });

        runner.SetPlayerObject(player, playerChar);
        Debug.Log($"{player}번 플레이어 스폰 완료 (위치: {xPos}) (모델: {assignedModelNum}) (팀: {assignedTeam})");
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

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
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
