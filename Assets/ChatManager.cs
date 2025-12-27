using UnityEngine;
using Photon.Chat;
using AuthenticationValues = Photon.Chat.AuthenticationValues;
using ExitGames.Client.Photon;
using Fusion;

public class ChatManager : MonoBehaviour, IChatClientListener
{
    [Header("설정")]
    public string chatAppId; // 대시보드에서 복사한 App ID
    public string currentChannel = "Lobby"; // 입장할 채널 이름

    [Header("UI 스크립트 연결")]
    public NetworkUI networkUI; // UI에게 "화면에 띄워"라고 시키기 위해 필요

    private ChatClient chatClient;
    private string myNickName;

    public void ConnectWithNickName()
    {
        myNickName = DataManager.Instance.UserNickName;

        if (string.IsNullOrEmpty(myNickName))
        {
            Debug.LogError("닉네임이 없습니다. DataManager을 먼저 세팅하세요");
            return;
        }
        chatClient = new ChatClient(this);

        // 연결하기
        chatClient.Connect(chatAppId, "1.0", new AuthenticationValues(myNickName));
        Debug.Log("채팅 서버 연결 시도...");
    }

    public void EnterRoomChannel(string roomName)
    {
        if (chatClient == null) return;

        string[] channelsToUnsub = new string[] { currentChannel };
        chatClient.Unsubscribe(channelsToUnsub);

        currentChannel = roomName;

        chatClient.Subscribe(new string[] { currentChannel });

        networkUI.ReceiveMessage("System", $"[{currentChannel}] 채널로 이동");
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
        if (chatClient.CanChat) // 연결 상태 확인
        {
            chatClient.PublishMessage(currentChannel, message);
        }
    }

    public void OnConnected()
    {
        currentChannel = "Lobby";
        chatClient.Subscribe(new string[] { currentChannel });
        networkUI.ReceiveMessage("System", $"{myNickName}님 채팅 서버에 연결되었습니다.");
    }

    public void OnGetMessages(string channelName, string[] senders, object[] messages)
    {
        for (int i = 0; i < senders.Length; i++)
        {
            networkUI.ReceiveMessage(senders[i], messages[i].ToString());
        }
    }

    public void OnDisconnected()
    {
        string reason = "";
        if (chatClient != null)
        {
            reason = chatClient.DisconnectedCause.ToString();
        }

        networkUI.ReceiveMessage("System", $"연결이 끊어졌습니다. (이유: {reason})");
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