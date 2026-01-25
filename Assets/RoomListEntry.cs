using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System; // Action을 쓰기 위해 필요

public class RoomListEntry : MonoBehaviour
{
    public TextMeshProUGUI infoText; // 방 정보를 띄울 텍스트
    public Button joinButton;        // 입장 버튼

    // LobbyUI가 이 함수를 호출해서 정보를 채워줄 겁니다.
    public void SetInfo(string roomName, int current, int max, Action onJoinClick)
    {
        // 텍스트 변경
        infoText.text = $"{roomName} ({current}/{max})";

        // 버튼 클릭 시 실행할 기능 연결
        joinButton.onClick.RemoveAllListeners(); // 기존 연결 삭제(중복 방지)
        joinButton.onClick.AddListener(() => onJoinClick());
    }
}