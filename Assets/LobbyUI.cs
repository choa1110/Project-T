using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using System.Collections.Generic;
using Fusion.Sockets;
using System.Threading.Tasks;

public class LobbyUI : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("UI 연결")]
    public GameObject mainPanel; // 방 생성, 방 입장
    public GameObject createRoomPanel; // 방 만들기 팝업
    public GameObject joinRoomPanel;   // 방 목록 팝업
    [Header("내부 컴포넌트")]
    public TMP_InputField roomNameInput;
    public Transform roomListContent;  
    public Button roomItemPrefab;      

    [Header("Room Settings")]
    public TMP_Dropdown roundsDropdown; // 1, 3, 5
    public TMP_Dropdown livesDropdown;  // 1, 2, 3
    public TMP_InputField timeInput;    // Manual input for seconds
    public TMP_Dropdown gameModeDropdown; // Solo, Team (2vs2)

    private NetworkRunner _runner;

    void Start()
    {
        // 시작하자마자 러너 초기화 (로비 접속은 버튼 누를 때 함)
        if (_runner == null)
        {
            _runner = gameObject.AddComponent<NetworkRunner>();
            _runner.ProvideInput = true;
            _runner.AddCallbacks(this);
        }
        
        OnBackToMain();
    }

    public void OnOpenCreatePanel()
    {
        mainPanel.SetActive(false);
        createRoomPanel.SetActive(true); // 생성 창 보이게
        joinRoomPanel.SetActive(false);
    }

    public async void OnOpenJoinPanel()
    {
        mainPanel.SetActive(false);
        createRoomPanel.SetActive(false);
        joinRoomPanel.SetActive(true); // 목록 창 보이게

        Debug.Log("로비 접속 시도중..");
        await _runner.JoinSessionLobby(SessionLobby.ClientServer);
    }

    public void OnBackToMain()
    {
        createRoomPanel.SetActive(false);
        joinRoomPanel.SetActive(false);
        mainPanel.SetActive(true); // 메인만 보이게
    }

    // 방 만들기
    public async void OnCreateRoomConfirm()
    {
        string rName = roomNameInput.text;
        if (string.IsNullOrEmpty(rName)) return;

        // Get values from dropdowns
        int rounds = roundsDropdown != null ? int.Parse(roundsDropdown.options[roundsDropdown.value].text) : 3;
        int lives = livesDropdown != null ? int.Parse(livesDropdown.options[livesDropdown.value].text) : 3;
        
        // Parse time from InputField
        int time = 120; // Default
        if (timeInput != null && !string.IsNullOrEmpty(timeInput.text))
        {
            if (int.TryParse(timeInput.text, out int parsedTime))
            {
                time = parsedTime;
            }
        }

        var customProperties = new Dictionary<string, SessionProperty>();
        customProperties.Add("Rounds", rounds);
        customProperties.Add("Lives", lives);
        customProperties.Add("Time", time);

        int gameModeVal = gameModeDropdown != null ? gameModeDropdown.value : 0;
        customProperties.Add("GameMode", gameModeVal);

        await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Host,
            SessionName = rName,
            Scene = SceneRef.FromIndex(1), 
            PlayerCount = 4,
            SessionProperties = customProperties,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
    }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        Debug.Log($"방 목록 갱신됨! 개수: {sessionList.Count}");

        // 1. 기존 목록 삭제
        foreach (Transform child in roomListContent) Destroy(child.gameObject);

        // 2. 새로운 버튼 생성
        foreach (SessionInfo session in sessionList)
        {
            if (!session.IsVisible || !session.IsOpen) continue;
            if (session.PlayerCount >= session.MaxPlayers) continue;

            var btnObj = Instantiate(roomItemPrefab, roomListContent);
            
            btnObj.transform.localScale = Vector3.one;
            btnObj.transform.localPosition = Vector3.zero;

            var entry = btnObj.GetComponent<RoomListEntry>();
            
            if (entry != null)
            {
                entry.SetInfo(session.Name, session.PlayerCount, session.MaxPlayers, async () => 
                {
                    // 클릭 시 실행될 내용
                    Debug.Log($"{session.Name} 방 입장 시도");
                    await _runner.StartGame(new StartGameArgs()
                    {
                        GameMode = GameMode.Client,
                        SessionName = session.Name,
                        Scene = SceneRef.FromIndex(1),
                        SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
                    });
                });
            }
        }
    }
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
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