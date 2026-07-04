using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using System.Collections.Generic;
using Fusion.Sockets;

public class LobbyUIController : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("패널")]
    public GameObject mainPanel;
    public GameObject createRoomPanel;
    public GameObject joinRoomPanel;
    public GameObject mapSelectPanel;

    [Header("방 만들기")]
    public TMP_InputField roomNameInput;

    [Header("방 목록")]
    public Transform roomListContent;
    public Button roomItemPrefab;

    NetworkRunner _runner;
    string _pendingRoomName;

    void Start()
    {
        if (_runner == null)
        {
            _runner = gameObject.AddComponent<NetworkRunner>();
            _runner.ProvideInput = true;
            _runner.AddCallbacks(this);
        }
        if (mainPanel       != null) mainPanel.SetActive(true);
        if (createRoomPanel != null) createRoomPanel.SetActive(false);
        if (joinRoomPanel   != null) joinRoomPanel.SetActive(false);
        if (mapSelectPanel  != null) mapSelectPanel.SetActive(false);
    }

    public void OnOpenCreatePanel()
    {
        if (mainPanel       != null) mainPanel.SetActive(false);
        if (createRoomPanel != null) createRoomPanel.SetActive(true);
        if (joinRoomPanel   != null) joinRoomPanel.SetActive(false);
        if (mapSelectPanel  != null) mapSelectPanel.SetActive(false);
    }

    public async void OnOpenJoinPanel()
    {
        if (mainPanel       != null) mainPanel.SetActive(false);
        if (createRoomPanel != null) createRoomPanel.SetActive(false);
        if (joinRoomPanel   != null) joinRoomPanel.SetActive(true);
        if (mapSelectPanel  != null) mapSelectPanel.SetActive(false);
        await _runner.JoinSessionLobby(SessionLobby.ClientServer);
    }

    public void OnBackToMain()
    {
        if (mainPanel       != null) mainPanel.SetActive(true);
        if (createRoomPanel != null) createRoomPanel.SetActive(false);
        if (joinRoomPanel   != null) joinRoomPanel.SetActive(false);
        if (mapSelectPanel  != null) mapSelectPanel.SetActive(false);
    }

public void OnMapSelectBack()
    {
        if (mainPanel       != null) mainPanel.SetActive(false);
        if (createRoomPanel != null) createRoomPanel.SetActive(true);
        if (joinRoomPanel   != null) joinRoomPanel.SetActive(false);
        if (mapSelectPanel  != null) mapSelectPanel.SetActive(false);
    }

public void OnCreateRoomConfirm()
    {
        string n = roomNameInput != null ? roomNameInput.text.Trim() : "";
        if (string.IsNullOrEmpty(n)) { Debug.LogWarning("[LobbyUI] 방 이름 필요"); return; }
        _pendingRoomName = n;
        if (mainPanel       != null) mainPanel.SetActive(false);
        if (createRoomPanel != null) createRoomPanel.SetActive(false);
        if (joinRoomPanel   != null) joinRoomPanel.SetActive(false);
        if (mapSelectPanel  != null)
        {
            mapSelectPanel.SetActive(true);
            var mapUI = mapSelectPanel.GetComponentInChildren<LobbyMapSelectUI>(true);
            if (mapUI != null) mapUI.Open(isHost: true);
        }
    }

    public async void OnStartGame()
    {
        if (string.IsNullOrEmpty(_pendingRoomName))
        {
            if (mainPanel != null) mainPanel.SetActive(true);
            return;
        }
        Debug.Log("[LobbyUI] 게임 시작: " + _pendingRoomName);
        await _runner.StartGame(new StartGameArgs
        {
            GameMode     = GameMode.Host,
            SessionName  = _pendingRoomName,
            Scene        = SceneRef.FromIndex(2),
            PlayerCount  = 4,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        foreach (Transform c in roomListContent) Destroy(c.gameObject);
        foreach (SessionInfo s in sessionList)
        {
            if (s.PlayerCount >= s.MaxPlayers) continue;
            var btn = Instantiate(roomItemPrefab, roomListContent);
            btn.transform.localScale = Vector3.one;
            var e = btn.GetComponent<RoomListEntry>();
            if (e == null) continue;
            string sn = s.Name; int cur = s.PlayerCount; int max = s.MaxPlayers;
            e.SetInfo(sn, cur, max, async () =>
            {
                await _runner.StartGame(new StartGameArgs
                {
                    GameMode     = GameMode.Client,
                    SessionName  = sn,
                    Scene        = SceneRef.FromIndex(2),
                    SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
                });
            });
        }
    }

public void OnPlayerJoined(NetworkRunner r, PlayerRef p)
    {
        // 클라이언트로 참여한 경우 맵 선택 패널을 읽기 전용으로 열기
        bool isHost = r != null && r.IsServer;
        if (!isHost && mapSelectPanel != null)
        {
            if (mainPanel       != null) mainPanel.SetActive(false);
            if (createRoomPanel != null) createRoomPanel.SetActive(false);
            if (joinRoomPanel   != null) joinRoomPanel.SetActive(false);
            mapSelectPanel.SetActive(true);
            var mapUI = mapSelectPanel.GetComponentInChildren<LobbyMapSelectUI>(true);
            if (mapUI != null) mapUI.Open(isHost: false);
        }
    }
    public void OnPlayerLeft(NetworkRunner r, PlayerRef p) { }
    public void OnInput(NetworkRunner r, NetworkInput i) { }
    public void OnInputMissing(NetworkRunner r, PlayerRef p, NetworkInput i) { }
    public void OnShutdown(NetworkRunner r, ShutdownReason s) { }
    public void OnConnectedToServer(NetworkRunner r) { }
    public void OnDisconnectedFromServer(NetworkRunner r, NetDisconnectReason n) { }
    public void OnConnectRequest(NetworkRunner r, NetworkRunnerCallbackArgs.ConnectRequest q, byte[] t) { }
    public void OnConnectFailed(NetworkRunner r, NetAddress a, NetConnectFailedReason n) { }
    public void OnUserSimulationMessage(NetworkRunner r, SimulationMessagePtr m) { }
    public void OnCustomAuthenticationResponse(NetworkRunner r, Dictionary<string, object> d) { }
    public void OnHostMigration(NetworkRunner r, HostMigrationToken t) { }
    public void OnReliableDataReceived(NetworkRunner r, PlayerRef p, System.ArraySegment<byte> d) { }
    public void OnReliableDataReceived(NetworkRunner r, PlayerRef p, ReliableKey k, System.ArraySegment<byte> d) { }
    public void OnReliableDataProgress(NetworkRunner r, PlayerRef p, ReliableKey k, float f) { }
    public void OnSceneLoadDone(NetworkRunner r) { }
    public void OnSceneLoadStart(NetworkRunner r) { }
    public void OnObjectExitAOI(NetworkRunner r, NetworkObject o, PlayerRef p) { }
    public void OnObjectEnterAOI(NetworkRunner r, NetworkObject o, PlayerRef p) { }
}
