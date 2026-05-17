using UnityEngine;
using Photon.Chat;
using AuthenticationValues = Photon.Chat.AuthenticationValues;
using ExitGames.Client.Photon;
using Fusion;

public class ChatManager : MonoBehaviour, IChatClientListener
{
    public string chatAppId;
    public string currentChannel = "Lobby"; 

    public NetworkUI networkUI;
    private ChatClient chatClient;
    private string myNickName;

    void Start()
    {
        ConnectWithNickName();
    }

    public void ConnectWithNickName()
    {
        // Prevent connecting if already connected or in progress
        if (chatClient != null && chatClient.State != ChatState.Disconnected && chatClient.State != ChatState.Uninitialized)
        {
            Debug.Log($"[ChatManager] Already connecting or connected. State: {chatClient.State}");
            return;
        }

        myNickName = DataManager.Instance.UserNickName;
        
        if (string.IsNullOrEmpty(myNickName))
        {
            // Don't log error here if it's expected (RoomPlayer will trigger it later)
            Debug.Log("[ChatManager] Nickname is empty. Waiting for RoomPlayer to set it.");
            return;
        }

        Debug.Log($"[ChatManager] Attempting to connect! Nickname: {myNickName}");

        if (string.IsNullOrEmpty(chatAppId))
        {
            Debug.LogError("[ChatManager] Chat App ID is empty! Check the Inspector.");
            return;
        }
        
        chatClient = new ChatClient(this);
        chatClient.Connect(chatAppId, "1.0", new AuthenticationValues(myNickName));
    }

    public void EnterRoomChannel(string roomName)
    {
        if (chatClient == null) return;

        string[] channelsToUnsub = new string[] { currentChannel };
        chatClient.Unsubscribe(channelsToUnsub);

        currentChannel = roomName;

        chatClient.Subscribe(new string[] { currentChannel });

        networkUI.ReceiveMessage("System", $"[{currentChannel}]");
    }

    void Update()
    {
        if (chatClient != null)
        {
            chatClient.Service();
        }
    }

    public void SendChatMessage(string message)
    {
        if (chatClient == null)
        {
            Debug.LogError("[ChatManager] chatClient가 null입니다! 연결이 아예 시작되지 않았습니다.");
            return;
        }

        if (chatClient.CanChat)
        {
            chatClient.PublishMessage(currentChannel, message);
            Debug.Log($"[ChatManager] 메시지 전송 요청: {message}");
        }
        else
        {
            Debug.LogWarning("[ChatManager] 서버와 연결 중이거나, 채팅을 칠 수 없는 상태입니다.");
        }
    }

    public void OnConnected()
    {
        currentChannel = "Lobby";
        chatClient.Subscribe(new string[] { currentChannel });
        if (networkUI != null) 
        {
            networkUI.ReceiveMessage("System", $"{myNickName}");
        }
    }

    public void OnGetMessages(string channelName, string[] senders, object[] messages)
    {
        for (int i = 0; i < senders.Length; i++)
        {
            if (networkUI != null) 
            {
                networkUI.ReceiveMessage(senders[i], messages[i].ToString());
            }
        }
    }

    public void OnDisconnected()
    {
        string reason = "";
        if (chatClient != null)
        {
            reason = chatClient.DisconnectedCause.ToString();
        }

        networkUI.ReceiveMessage("System", $"������ ���������ϴ�. (����: {reason})");
        Debug.LogError($"[Chat Error] Disconnect Cause: {reason}");
    }

    public void DebugReturn(DebugLevel level, string message) { }
    public void OnChatStateChange(ChatState state) { }
    public void OnPrivateMessage(string sender, object message, string channelName) { }
    public void OnSubscribed(string[] channels, bool[] results) { }
    public void OnUnsubscribed(string[] channels) { }
    public void OnStatusUpdate(string user, int status, bool gotMessage, object message) { }
    public void OnUserSubscribed(string channel, string user) { }
    public void OnUserUnsubscribed(string channel, string user) { }
}