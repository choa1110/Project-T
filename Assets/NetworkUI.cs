using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NetworkUI : MonoBehaviour
{
    public TMP_InputField inputField;
    public TextMeshProUGUI chatContent;
    public ScrollRect scrollRect;
    public ChatManager chatManager;

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
        if (this == null || !gameObject.activeInHierarchy) return;
        string formattedText = $"<color=yellow>[{senderName}]</color>: {message}";
        chatContent.text += formattedText + "\n";
        Invoke("ScrollToBottom", 0.1f);
    }

    void ScrollToBottom()
    {
        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 0f;
    }
}