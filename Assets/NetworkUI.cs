using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Fusion;
using System.Collections.Generic;
using System;
using Fusion.Sockets;

public class NetworkUI : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("UI 연결")]
    public TMP_InputField inputField;
    public TextMeshProUGUI chatContent;
    public ScrollRect scrollRect;

    [Header("매니저 연결")]
    public ChatManager chatManager;

    [Header("로그인 UI")]
    public GameObject loginPanel;
    public TMP_InputField idInput;

    private NetworkRunner _runner;

    private void Start()
    {
        string savedName = DataManager.Instance.LoadNickName();

        if (!string.IsNullOrEmpty(savedName))
        {
            idInput.text = savedName;
        }
    }

    public void OnLoginButtonClicked()
    {
        string inputName = idInput.text;
        if (string.IsNullOrEmpty(inputName)) return;

        DataManager.Instance.SetNickName(inputName);

        chatManager.ConnectWithNickName();

        loginPanel.SetActive(false);
    }

    public async void OnHostButtonClicked()
    {
        StartGame(GameMode.Host);
        string roomName = "Room_1";
        chatManager.EnterRoomChannel(roomName);
    }
    public async void OnJoinButtonClicked()
    {
        StartGame(GameMode.Client);
        string roomName = "Room_1";
        chatManager.EnterRoomChannel(roomName);
    }

    async void StartGame(GameMode mode)
    {
        if (_runner != null) return;

        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        _runner.AddCallbacks(this);

        await _runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = "Room_1",
            // Scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene(),
            PlayerCount = 4,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });

        Debug.Log($"Fusion 게임 시작! 모드: {mode}");
    }
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();

        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        data.direction = new Vector2(x, y);

        input.Set(data);
    }

    public void OnSendButtonClicked()
    {
        string msg = inputField.text;
        if (string.IsNullOrEmpty(msg)) return;

        chatManager.SendChatMessage(msg);
        inputField.text = "";
        inputField.ActivateInputField();
    }

    public void ReceiveMessage(string senderName, string message)
    {
        // 화면에 텍스트 추가
        string formattedText = $"<color=yellow>[{senderName}]</color>: {message}";
        chatContent.text += formattedText + "\n";
        Invoke("ScrollToBottom", 0.1f);
    }

    void ScrollToBottom()
    {
        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 0f;
    }




    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
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
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
    }
}


