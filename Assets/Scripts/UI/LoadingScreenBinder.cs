using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

/// <summary>
/// 기존 GameManager를 수정하지 않고, NetworkRunner의 씬 로드 콜백만 따로 구독해
/// LoadingScreen을 표시/숨김 처리하는 부가 컴포넌트.
/// 사용법: 로비 씬에 빈 GameObject 하나 만들고 이 스크립트를 붙이면 끝.
/// (Fusion은 한 Runner에 콜백 다중 구독을 허용하므로 GameManager 스폰 로직과 충돌하지 않음)
/// </summary>
public class LoadingScreenBinder : MonoBehaviour, INetworkRunnerCallbacks
{
    NetworkRunner _runner;
    bool _registered;

    void Awake() => DontDestroyOnLoad(gameObject);

    void Update()
    {
        if (_registered) return;

        if (_runner == null)
            _runner = FindFirstObjectByType<NetworkRunner>();

        if (_runner != null)
        {
            _runner.AddCallbacks(this);
            _registered = true;
        }
    }

    // --- 실제로 쓰는 두 콜백 ---
    public void OnSceneLoadStart(NetworkRunner runner)
    {
        LoadingScreen.Instance?.Show();
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        LoadingScreen.Instance?.Hide();
    }

    // --- 나머지 INetworkRunnerCallbacks: 빈 구현 ---
    public void OnPlayerJoined(NetworkRunner r, PlayerRef p) { }
    public void OnPlayerLeft(NetworkRunner r, PlayerRef p) { }
    public void OnInput(NetworkRunner r, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner r, PlayerRef p, NetworkInput input) { }
    public void OnShutdown(NetworkRunner r, ShutdownReason s) { }
    public void OnConnectedToServer(NetworkRunner r) { }
    public void OnDisconnectedFromServer(NetworkRunner r, NetDisconnectReason n) { }
    public void OnConnectRequest(NetworkRunner r, NetworkRunnerCallbackArgs.ConnectRequest req, byte[] t) { }
    public void OnConnectFailed(NetworkRunner r, NetAddress a, NetConnectFailedReason n) { }
    public void OnUserSimulationMessage(NetworkRunner r, SimulationMessagePtr m) { }
    public void OnSessionListUpdated(NetworkRunner r, List<SessionInfo> s) { }
    public void OnCustomAuthenticationResponse(NetworkRunner r, Dictionary<string, object> d) { }
    public void OnHostMigration(NetworkRunner r, HostMigrationToken t) { }
    public void OnReliableDataReceived(NetworkRunner r, PlayerRef p, ReliableKey k, ArraySegment<byte> d) { }
    public void OnReliableDataProgress(NetworkRunner r, PlayerRef p, ReliableKey k, float f) { }
    public void OnObjectExitAOI(NetworkRunner r, NetworkObject o, PlayerRef p) { }
    public void OnObjectEnterAOI(NetworkRunner r, NetworkObject o, PlayerRef p) { }
}
