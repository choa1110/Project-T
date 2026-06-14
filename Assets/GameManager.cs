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
            RoundTimer = 10f; //test는 30초로 , 180초로 수정해야됌
            _isCardUIOpened = false;
            randomBoxTimer = TickTimer.CreateFromSeconds(Runner, randomBoxSpawnInterval);
        }
    }

    [Networked] public int CurrentRound { get; set; } = 1 ;

    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority)
        {
            if (RoundTimer > 0 && !_isCardUIOpened)
            {
                RoundTimer -= Runner.DeltaTime;

                if (RoundTimer <= 0)
                {
                    RoundTimer = 0;
                    _isCardUIOpened = true;

                    //RPC_ShowCardUI(CurrentRound); 
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
            foreach (var player in runner.ActivePlayers)
            {
                SpawnGameCharacter(runner, player);
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

    void SpawnGameCharacter(NetworkRunner runner, PlayerRef player)
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

        NetworkObject playerCharacter = runner.Spawn(playerPrefab, spawnPosition, Quaternion.identity, player);
        runner.SetPlayerObject(player, playerCharacter);
        Debug.Log($"{player}번 플레이어 스폰 완료 (위치: {xPos})");
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
