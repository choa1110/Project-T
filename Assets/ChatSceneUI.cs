using UnityEngine;
using TMPro;
using Fusion;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;

public class ChatSceneUI : MonoBehaviour
{
    [Header("방 정보")]
    public TextMeshProUGUI roomNameText;
    
    [Header("플레이어 슬롯 (4개 연결)")]
    // ★ 텍스트 하나 대신, 슬롯 4개를 배열로 관리합니다.
    // 슬롯 1~4번의 'NameText'들을 순서대로 넣어주세요.
    public TextMeshProUGUI[] nameSlots;  
    
    // 슬롯 1~4번의 'StateText'들을 순서대로 넣어주세요.
    public TextMeshProUGUI[] stateSlots; 

    [Header("버튼 연결")]
    public Button startButton;             
    public Button readyButton;             
    public Button leaveButton;             

    private NetworkRunner _runner;

    void Start()
    {
        _runner = FindFirstObjectByType<NetworkRunner>();
        
        if (_runner != null && _runner.SessionInfo != null)
            roomNameText.text = $"방: {_runner.SessionInfo.Name}";

        if (leaveButton) leaveButton.onClick.AddListener(OnLeaveClicked);
        if (startButton) startButton.onClick.AddListener(OnStartClicked);
        if (readyButton) readyButton.onClick.AddListener(OnReadyClicked);
    }

    void Update()
    {
        if (_runner == null) return;

        UpdatePlayerSlots(); // ★ 슬롯 갱신 함수로 변경
        UpdateButtons();    
    }

    // ★ [핵심] 4개의 슬롯을 현재 인원수에 맞춰서 갱신
    void UpdatePlayerSlots()
    {
        // 1. 현재 접속한 플레이어 리스트 가져오기
        var players = RoomPlayer.Players; // (RoomPlayer.cs의 static 리스트)

        // 2. 슬롯 4개를 순회하면서 채워넣기
        for (int i = 0; i < 4; i++)
        {
            // 배열 범위 체크 (혹시 연결 안 했을까봐)
            if (i >= nameSlots.Length || i >= stateSlots.Length) break;

            if (i < players.Count) 
            {
                var p = players[i];
                nameSlots[i].text = p.NickName;

                // 상태 표시 (방장 / 레디 / 대기)
                if (p.IsLeader) 
                    stateSlots[i].text = "<color=orange>HOST</color>";
                else if (p.IsReady) 
                    stateSlots[i].text = "<color=green>READY</color>";
                else 
                    stateSlots[i].text = "<color=red>WAIT</color>";
                
                // (선택) 배경 이미지가 있다면 활성화
                if(nameSlots[i].text != null)
                    nameSlots[i].transform.parent.gameObject.SetActive(true);
            }
            else
            {
                nameSlots[i].text = "빈 자리";
                stateSlots[i].text = "-";
            }
        }
    }

    void UpdateButtons()
    {
        if (_runner.IsServer)
        {
            readyButton.gameObject.SetActive(false); 
            startButton.gameObject.SetActive(true);  
            bool allReady = RoomPlayer.Players.Where(p => !p.Object.HasStateAuthority).All(p => p.IsReady);
            startButton.interactable = allReady || RoomPlayer.Players.Count == 1; 
        }
        else 
        {
            startButton.gameObject.SetActive(false); 
            readyButton.gameObject.SetActive(true);  
            var myPlayer = RoomPlayer.Players.FirstOrDefault(p => p.Object.HasInputAuthority);
            if (myPlayer != null)
            {
                readyButton.GetComponentInChildren<TextMeshProUGUI>().text = myPlayer.IsReady ? "준비 취소" : "준비 완료";
            }
        }
    }

    public void OnReadyClicked()
    {
        var myPlayer = RoomPlayer.Players.FirstOrDefault(p => p.Object.HasInputAuthority);
        if (myPlayer != null) myPlayer.RPC_SetReady(!myPlayer.IsReady); 
    }

    public void OnStartClicked()
    {
        _runner.LoadScene(SceneRef.FromIndex(2));
    }

    public void OnLeaveClicked()
    {
        if (_runner != null) _runner.Shutdown();
        SceneManager.LoadScene(0);
    }
}