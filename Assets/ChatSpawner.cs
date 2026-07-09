using UnityEngine;
using Fusion;
using System.Collections.Generic;
using Fusion.Sockets;
public class ChatSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    public NetworkPrefabRef sessionPlayerPrefab; 

    private NetworkRunner _runner;
    private HashSet<PlayerRef> _spawningPlayers = new HashSet<PlayerRef>();

    void Start()
    {
        _runner = FindFirstObjectByType<NetworkRunner>();
        
        if (_runner == null) return;

        _runner.AddCallbacks(this);

        if (_runner.IsServer)
        {
            foreach (var player in _runner.ActivePlayers)
            {
                SpawnPlayer(player);
            }
        }
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (_runner.IsServer)
        {
            SpawnPlayer(player);
        }
    }

    void SpawnPlayer(PlayerRef player)
    {
        // Prevent duplicate spawning
        foreach (var rp in RoomPlayer.Players)
        {
            if (rp != null && rp.Object != null && rp.Object.IsValid && rp.Object.InputAuthority == player)
            {
                _spawningPlayers.Remove(player);
                return;
            }
        }

        if (_spawningPlayers.Contains(player))
        {
            return;
        }

        _spawningPlayers.Add(player);
        _runner.SpawnAsync(sessionPlayerPrefab, Vector3.zero, Quaternion.identity, player);
        Debug.Log($"{player}번 플레이어 ChatScene 스폰 완료");
    }

    void OnDestroy()
    {
        if (_runner != null) _runner.RemoveCallbacks(this);
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        _spawningPlayers.Remove(player);
    }
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
    public void OnSceneLoadDone(NetworkRunner runner)
    {
        if (runner.IsServer)
        {
            foreach (var player in runner.ActivePlayers)
            {
                SpawnPlayer(player);
            }
        }
    }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}