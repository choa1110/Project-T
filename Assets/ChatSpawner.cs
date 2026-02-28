// using UnityEngine;
// using Fusion;

// // 역할: ChatScene(대기실)에 도착하면 플레이어 명찰(SessionPlayer)을 생성해줌
// public class ChatSpawner : MonoBehaviour, IPlayerJoined
// {
//     public NetworkPrefabRef sessionPlayerPrefab; // SessionPlayer 프리팹 연결


//     void Start()
//     {
//         // 씬 로드 직후, 방장이라면 현재 있는 플레이어들의 명찰을 다 만들어줌
//         var runner = FindObjectOfType<NetworkRunner>();
//         if (runner != null && runner.IsServer)
//         {
//             foreach (var player in runner.ActivePlayers)
//             {
//                 runner.Spawn(sessionPlayerPrefab, Vector3.zero, Quaternion.identity, player);
//             }
//         }
//     }

//     // 누군가 뒤늦게 들어왔을 때 실행됨
//     public void PlayerJoined(PlayerRef player)
//     {
//         var runner = FindObjectOfType<NetworkRunner>();
//         if (runner.IsServer)
//         {
//             runner.Spawn(sessionPlayerPrefab, Vector3.zero, Quaternion.identity, player);
//         }
//     }
// }

using UnityEngine;
using Fusion;
using System.Collections.Generic;
using Fusion.Sockets;
// 역할: 씬이 시작될 때 + 누군가 들어올 때 캐릭터(명찰) 생성
public class ChatSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    // ★ GameObject 대신 NetworkPrefabRef를 쓰는 게 Fusion 권장 사항입니다.
    public NetworkPrefabRef sessionPlayerPrefab; 

    private NetworkRunner _runner;

    void Start()
    {
        _runner = FindObjectOfType<NetworkRunner>();
        
        if (_runner == null) return;

        _runner.AddCallbacks(this);
    }

    // 누군가 새로 들어왔을 때 (게임 도중 난입)
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (_runner.IsServer)
        {
            SpawnPlayer(player);
        }
    }

    // 중복 코드를 줄이기 위해 함수로 분리
    void SpawnPlayer(PlayerRef player)
    {
        // 이미 생성된 캐릭터가 겹칠 수 있으니 위치를 살짝 분산시키거나 0,0,0에 둠
        // _runner.Spawn(sessionPlayerPrefab, Vector3.zero, Quaternion.identity, player);
        _runner.SpawnAsync(sessionPlayerPrefab, Vector3.zero, Quaternion.identity, player);
        Debug.Log($"{player}번 플레이어 ChatScene 스폰 완료");
    }

    // OnDestroy 때 콜백 해제 (에러 방지)
    void OnDestroy()
    {
        if (_runner != null) _runner.RemoveCallbacks(this);
    }

    // --- 인터페이스 구현 (나머지는 빈칸 유지) ---
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, System.ArraySegment<byte> data) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}