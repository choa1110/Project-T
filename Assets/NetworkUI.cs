using TMPro;
using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class NetworkUI : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("채팅 UI 연결")]
    public TMP_InputField inputField;
    public TextMeshProUGUI chatContent;
    public ScrollRect scrollRect;
    public ChatManager chatManager;

    [Header("게임 캐릭터 (SampleScene용)")]
    public NetworkPrefabRef playerPrefab;

    private NetworkRunner _runner;

    private void Start()
    {
        // 이미 Lobby에서 생성된 Runner를 찾아서 연결
        _runner = FindObjectOfType<NetworkRunner>();
        if (_runner != null)
        {
            _runner.AddCallbacks(this);
        }
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

        if(chatManager != null) chatManager.SendChatMessage(msg);
        
        inputField.text = "";
        inputField.ActivateInputField();
    }

    public void ReceiveMessage(string senderName, string message)
    {
        string formattedText = $"<color=yellow>[{senderName}]</color>: {message}";
        chatContent.text += formattedText + "\n";
        Invoke("ScrollToBottom", 0.1f);
    }

    void ScrollToBottom()
    {
        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 0f;
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
            if (SceneManager.GetActiveScene().buildIndex == 2) 
            {
                int uniqueId = player.RawEncoded;
                float xPos = (uniqueId % 10) * 2f;
                Vector3 spawnPosition = new Vector3(xPos, 2f, 0);
                
                runner.Spawn(playerPrefab, spawnPosition, Quaternion.identity, player);
                Debug.Log($"{player}번 플레이어 게임 캐릭터 생성됨!");
            }
        }
    }

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
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data){ }
}