using UnityEngine;
using Photon.Chat;
using AuthenticationValues = Photon.Chat.AuthenticationValues;
using ExitGames.Client.Photon;
using Fusion;

public class ChatManager : MonoBehaviour, IChatClientListener
{
    [Header("����")]
    public string chatAppId; // ��ú��忡�� ������ App ID
    public string currentChannel = "Lobby"; // ������ ä�� �̸�

    [Header("UI ��ũ��Ʈ ����")]
    public NetworkUI networkUI; // UI���� "ȭ�鿡 ���"��� ��Ű�� ���� �ʿ�

    private ChatClient chatClient;
    private string myNickName;

    public void ConnectWithNickName()
    {
        myNickName = DataManager.Instance.UserNickName;

        if (string.IsNullOrEmpty(myNickName))
        {
            return;
        }
        chatClient = new ChatClient(this);

        chatClient.Connect(chatAppId, "1.0", new AuthenticationValues(myNickName));
        Debug.Log("ä�� ���� ���� �õ�...");
    }

    public void EnterRoomChannel(string roomName)
    {
        if (chatClient == null) return;

        string[] channelsToUnsub = new string[] { currentChannel };
        chatClient.Unsubscribe(channelsToUnsub);

        currentChannel = roomName;

        chatClient.Subscribe(new string[] { currentChannel });

        networkUI.ReceiveMessage("System", $"[{currentChannel}] ä�η� �̵�");
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
        if (chatClient.CanChat) // ���� ���� Ȯ��
        {
            chatClient.PublishMessage(currentChannel, message);
        }
    }

    public void OnConnected()
    {
        currentChannel = "Lobby";
        chatClient.Subscribe(new string[] { currentChannel });
        networkUI.ReceiveMessage("System", $"{myNickName}�� ä�� ������ ����Ǿ����ϴ�.");
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