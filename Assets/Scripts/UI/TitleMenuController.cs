using UnityEngine;

/// <summary>
/// 타이틀(메인) 화면 제어. 게임 진입 시 타이틀을 먼저 띄우고,
/// 게임시작 누르면 로비 메뉴(MainPanel)로 전환. 설정 패널 열고/닫기, 게임 종료.
/// </summary>
public class TitleMenuController : MonoBehaviour
{
    [SerializeField] GameObject titlePanel;     // 새 타이틀 화면
    [SerializeField] GameObject lobbyPanel;     // 기존 MainPanel (방 생성/참가)
    [SerializeField] GameObject settingsPanel;  // 설정 창

    void Start()
    {
        if (titlePanel) titlePanel.SetActive(true);
        if (lobbyPanel) lobbyPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
    }

    public void OnStartGame()
    {
        if (settingsPanel) settingsPanel.SetActive(false);
        if (titlePanel) titlePanel.SetActive(false);
        if (lobbyPanel) lobbyPanel.SetActive(true);
    }

    public void OnOpenSettings()  { if (settingsPanel) settingsPanel.SetActive(true); }
    public void OnCloseSettings() { if (settingsPanel) settingsPanel.SetActive(false); }

    public void OnQuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
