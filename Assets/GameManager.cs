using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

public class GameManager : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkRunner _runner;

    public NetworkPrefabRef playerPrefab;

    private void Start()
    {
        _runner = FindFirstObjectByType<NetworkRunner>();
        if (_runner != null)
        {
            _runner.AddCallbacks(this);
            Debug.Log("<color=green>GameManager: Runner 찾음! 콜백 등록 완료!</color>");        
            }
        
        else {
            Debug.LogError("<color=red>GameManager: 심각한 문제! NetworkRunner를 찾을 수 없습니다!</color>");
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
