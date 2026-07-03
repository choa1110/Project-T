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
    public TextMeshProUGUI[] nameSlots;  
    
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
        {
            string modeStr = "Solo";
            if (_runner.SessionInfo.Properties.TryGetValue("GameMode", out var gmProp) && (int)gmProp == 1)
            {
                modeStr = "2vs2 Team";
            }
            roomNameText.text = $"방: {_runner.SessionInfo.Name} ({modeStr})";
        }

        if (leaveButton) leaveButton.onClick.AddListener(OnLeaveClicked);
        if (startButton) startButton.onClick.AddListener(OnStartClicked);
        if (readyButton) readyButton.onClick.AddListener(OnReadyClicked);
    }

    void Update()
    {
        if (_runner == null) return;

        UpdatePlayerSlots();
        UpdateButtons();    

        // Press 'T' key to switch teams in Team Mode
        if (Input.GetKeyDown(KeyCode.T))
        {
            ToggleMyTeam();
        }
    }

    void ToggleMyTeam()
    {
        bool isTeamMatch = false;
        if (_runner != null && _runner.SessionInfo != null && 
            _runner.SessionInfo.Properties.TryGetValue("GameMode", out var gmProp))
        {
            isTeamMatch = (int)gmProp == 1;
        }

        if (isTeamMatch)
        {
            var myPlayer = RoomPlayer.Players.FirstOrDefault(p => p.Object.HasInputAuthority);
            if (myPlayer != null)
            {
                myPlayer.RPC_ToggleTeam();
            }
        }
    }

    void UpdatePlayerSlots()
    {
        var players = RoomPlayer.Players;

        bool isTeamMatch = false;
        if (_runner != null && _runner.SessionInfo != null && 
            _runner.SessionInfo.Properties.TryGetValue("GameMode", out var gmProp))
        {
            isTeamMatch = (int)gmProp == 1;
        }

        // Initialize slots as empty/placeholder
        for (int i = 0; i < 4; i++)
        {
            if (i >= nameSlots.Length || i >= stateSlots.Length) break;
            nameSlots[i].text = "빈 자리";
            stateSlots[i].text = "-";
        }

        if (isTeamMatch)
        {
            int redCount = 0;
            int blueCount = 0;

            foreach (var p in players)
            {
                if (p.Team == 0) // RED Team
                {
                    int slotIndex = 0 + redCount;
                    if (slotIndex < 2 && slotIndex < nameSlots.Length)
                    {
                        nameSlots[slotIndex].text = $"<color=red>[RED]</color> {p.NickName}";
                        stateSlots[slotIndex].text = GetStateText(p);
                        if (nameSlots[slotIndex].transform.parent != null)
                            nameSlots[slotIndex].transform.parent.gameObject.SetActive(true);
                        redCount++;
                    }
                }
                else // BLUE Team
                {
                    int slotIndex = 2 + blueCount;
                    if (slotIndex < 4 && slotIndex < nameSlots.Length)
                    {
                        nameSlots[slotIndex].text = $"<color=blue>[BLUE]</color> {p.NickName}";
                        stateSlots[slotIndex].text = GetStateText(p);
                        if (nameSlots[slotIndex].transform.parent != null)
                            nameSlots[slotIndex].transform.parent.gameObject.SetActive(true);
                        blueCount++;
                    }
                }
            }
        }
        else
        {
            // Solo match: display in join order
            for (int i = 0; i < players.Count; i++)
            {
                if (i >= nameSlots.Length || i >= stateSlots.Length) break;
                var p = players[i];
                nameSlots[i].text = p.NickName;
                stateSlots[i].text = GetStateText(p);
                if (nameSlots[i].transform.parent != null)
                    nameSlots[i].transform.parent.gameObject.SetActive(true);
            }
        }
    }

    string GetStateText(RoomPlayer p)
    {
        if (p.IsLeader) 
            return "<color=orange>HOST</color>";
        else if (p.IsReady) 
            return "<color=green>READY</color>";
        else 
            return "<color=red>WAIT</color>";
    }

    void UpdateButtons()
    {
        if (_runner.IsServer)
        {
            readyButton.gameObject.SetActive(false); 
            startButton.gameObject.SetActive(true);  

            bool isTeamMatch = false;
            if (_runner != null && _runner.SessionInfo != null && 
                _runner.SessionInfo.Properties.TryGetValue("GameMode", out var gmProp))
            {
                isTeamMatch = (int)gmProp == 1;
            }

            bool allReady = RoomPlayer.Players.Where(p => !p.Object.HasInputAuthority).All(p => p.IsReady);

            if (isTeamMatch)
            {
                // Team Match requires exactly 4 players, and all clients must be ready
                startButton.interactable = (RoomPlayer.Players.Count == 4) && allReady;
            }
            else
            {
                // Solo match requires all clients to be ready (allows starting with 1 player for local testing)
                startButton.interactable = allReady || RoomPlayer.Players.Count == 1; 
            }
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
        bool isTeamMatch = false;
        if (_runner != null && _runner.SessionInfo != null && 
            _runner.SessionInfo.Properties.TryGetValue("GameMode", out var gmProp))
        {
            isTeamMatch = (int)gmProp == 1;
        }

        // Only start if all client players are ready
        bool allReady = RoomPlayer.Players.Where(p => !p.Object.HasInputAuthority).All(p => p.IsReady);
        
        if (isTeamMatch)
        {
            if (RoomPlayer.Players.Count < 4)
            {
                Debug.LogWarning("Cannot start Team Match: Needs 4 players!");
                return;
            }
            if (!allReady)
            {
                Debug.LogWarning("Cannot start Team Match: Not all players are ready!");
                return;
            }
        }
        else
        {
            if (!allReady && RoomPlayer.Players.Count > 1)
            {
                Debug.LogWarning("Cannot start game: Not all players are ready!");
                return;
            }
        }

        if (_runner.IsServer && _runner.SessionInfo != null)
        {
            _runner.SessionInfo.IsOpen = false;
            _runner.SessionInfo.IsVisible = false;
        }
        _runner.LoadScene(SceneRef.FromIndex(2));
    }

    public void OnLeaveClicked()
    {
        if (_runner != null) _runner.Shutdown();
        SceneManager.LoadScene(0);
    }
}