using UnityEngine;
using UnityEngine.UI;
using Fusion;

/// <summary>
/// ChatScene(방 대기실) 제어. "맵 선택" 버튼으로 맵선택 패널을 열고(호스트/참가자 구분),
/// 패널의 StartButton으로 호스트가 게임 씬(SampleScene)으로 전환한다.
/// 네트워크 러너는 로비에서 DontDestroyOnLoad로 넘어온 것을 찾아 쓴다.
/// </summary>
public class ChatRoomController : MonoBehaviour
{
    [SerializeField] GameObject mapSelectPanel;
    [SerializeField] LobbyMapSelectUI mapUI;
    [SerializeField] Button startButton;      // 패널 안의 StartButton

    const int GameSceneIndex = 3; // SampleScene

    NetworkRunner _runner;
    bool _isHost;

    void Start()
    {
        _runner = FindFirstObjectByType<NetworkRunner>();
        _isHost = _runner == null || _runner.IsServer; // 러너 없으면(단독 테스트) 호스트 취급

        if (mapSelectPanel != null) mapSelectPanel.SetActive(false);

        if (startButton != null)
        {
            startButton.onClick.RemoveListener(OnStartGame);
            startButton.onClick.AddListener(OnStartGame);
        }
    }

    // "맵 선택" 버튼 onClick 에서 호출
    public void OpenPanel()
    {
        if (mapSelectPanel != null) mapSelectPanel.SetActive(true);
        if (mapUI != null) mapUI.Open(_isHost);
    }

    // 패널 StartButton -> 호스트만 게임 씬 로드
    public void OnStartGame()
    {
        if (_runner == null) { Debug.LogWarning("[ChatRoom] NetworkRunner 없음"); return; }
        if (!_runner.IsServer) return;
        _runner.LoadScene(SceneRef.FromIndex(GameSceneIndex));
    }
}
