using System;
using System.Collections.Generic;
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

    private NetworkRunner _runner;

    public NetworkPrefabRef playerPrefab;

    [Networked] public float RoundTimer { get; set; }
    [Networked] public int CurrentRound { get; set; }
    public CardUI cardUI;
    bool _isCardUIOpened = false;

    [Header("Item Box Spawner")]
    public NetworkPrefabRef itemBoxPrefab;
    public float boxSpawnInterval = 8f;
    private float _boxSpawnTimer;

    [Header("Item UI")]
    public ItemSlot[] itemSlots;

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

        _runner = FindFirstObjectByType<NetworkRunner>();

        if (_runner != null)
        {
            _runner.AddCallbacks(this);
            Debug.Log("<color=green>GameManager: Runner 찾음! 콜백 등록 완료!</color>");
        }
        else
        {
            Debug.LogError("<color=red>GameManager: 심각한 문제! NetworkRunner를 찾을 수 없습니다!</color>");
        }

        var map = inputActions.FindActionMap("Player");

        Debug.Log(map.FindAction("Move"));
        Debug.Log(map.FindAction("Jump"));
        Debug.Log(map.FindAction("Attack"));
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

            CurrentRound = 1;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority && RoundTimer > 0 && !_isCardUIOpened)
        {
            RoundTimer -= Runner.DeltaTime;

            if (RoundTimer <= 0)
            {
                RoundTimer = 0;
                _isCardUIOpened = true;
                
                // RPC_ShowCardUI(CurrentRound); 
            }
        }
        if (Object.HasStateAuthority)
        {
            _boxSpawnTimer -= Runner.DeltaTime;

            if (_boxSpawnTimer <= 0)
            {
                _boxSpawnTimer = boxSpawnInterval;
                SpawnRandomItemBox();
            }
        }
    }

    private void SpawnRandomItemBox()
    {
        float randomX = UnityEngine.Random.Range(-25f, 25f);
        float randomZ = UnityEngine.Random.Range(-25f, 25f);
        
        Vector3 spawnPos = new Vector3(randomX, 1f, randomZ);

        Runner.Spawn(itemBoxPrefab, spawnPos, Quaternion.identity);
        
        Debug.Log($"[서버] 아이템 상자 생성됨! 위치: {spawnPos}");
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

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
            SpawnGameCharacter(runner, player);
        }
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
        runner.Spawn(playerPrefab, spawnPosition, Quaternion.identity, player);
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
