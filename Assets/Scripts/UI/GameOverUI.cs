using UnityEngine;
using TMPro;
using Fusion;
using UnityEngine.SceneManagement;

/// <summary>
/// 최종 라운드 종료 시 승자를 표시하는 게임오버 패널.
/// GameManager.RPC_GameOver 에서 Show(...)가 호출된다(전 클라이언트).
/// </summary>
public class GameOverUI : MonoBehaviour
{
    public static GameOverUI Instance;

    [SerializeField] GameObject panel;
    [SerializeField] TMP_Text winnerText;

    void Awake()
    {
        Instance = this;
        if (panel) panel.SetActive(false);
    }

    public void Show(string winnerName, int winnerScore)
    {
        if (winnerText) winnerText.text = $"승자: {winnerName}\n{winnerScore}점";
        if (panel) panel.SetActive(true);
    }

    // 로비로 버튼
    public void OnBackToLobby()
    {
        var runner = FindFirstObjectByType<NetworkRunner>();
        if (runner != null) runner.Shutdown();
        SceneManager.LoadScene(0); // 로비 = 빌드 인덱스 0
    }
}
