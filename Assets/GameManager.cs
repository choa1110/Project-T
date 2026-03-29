using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

public class GameManager : NetworkBehaviour, INetworkRunnerCallbacks
{
    public static GameManager instance; // Singleton
    private NetworkRunner _networkRunner;
    public NetworkPrefabRef playerPrefab;

    [Networked] public float RoundTimer { get; set; }

    // 수정 2: [Networked] 속성이 있는 프로퍼티는 선언부에서 초기화(= 1;)하면 
    // Fusion 에러가 발생할 수 있으므로 선언만 하고 Spawned()에서 초기화합니다.
    [Networked] public int CurrentRound { get; set; }

    public CardUI cardUI;
    private bool _isCardUIOpened = false;

    //박스 생성 추가
    [Header("Item Box Spawner")]
    public NetworkPrefabRef itemBoxPrefab;
    public float boxSpawnInterval = 8f;
    private float _boxSpawnTimer;

    [Header("Item UI")]
    public ItemSlot[] itemSlots;
    
    private void Start()
    {
        instance = this;
        _networkRunner = FindFirstObjectByType<NetworkRunner>();
        if (_networkRunner != null)
        {
            _networkRunner.AddCallbacks(this);
            Debug.Log("<color=green>GameManager: Runner 찾음! 콜백 등록 완료!</color>");        
            }
        
        else {
            Debug.LogError("<color=red>GameManager: 심각한 문제! NetworkRunner를 찾을 수 없습니다!</color>");
        }
    }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            RoundTimer = 10f; // test는 30초로, 나중엔 180초로 수정
            _isCardUIOpened = false;

            // 네트워크 변수 초기화는 여기서 진행
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
        if (cardUI != null)
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

        // 위치 계산
        int index = player.PlayerId;
        float xPos = index * 2f;
        Vector3 spawnPosition = new Vector3(xPos, 3f, 0);

        // 스폰 실행
        Debug.Log(this.name + " : " + playerPrefab);
        runner.Spawn(playerPrefab, spawnPosition, Quaternion.identity, player);
        Debug.Log($"{player}번 플레이어 스폰 완료 (위치: {xPos})");
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        Debug.Log("입력 보내는중");
        var data = new NetworkInputData();

        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        data.direction = new Vector2(x, y);

        if (Input.GetKey(KeyCode.Space))
            data.buttons.Set(InputButtons.Jump, true);

        if (Input.GetMouseButton(0))
            data.buttons.Set(InputButtons.Attack, true);

        input.Set(data);
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
}