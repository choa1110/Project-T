using UnityEngine;
using TMPro;

public class RoundTimerUI : MonoBehaviour
{
    public TMP_Text roundLabel;
    public TMP_Text timeLabel;

    void Update()
    {
        if (GameManager.Instance == null) return;

        float t = Mathf.Max(0f, GameManager.Instance.RoundTimer);
        int round = GameManager.Instance.CurrentRound;

        if (roundLabel != null)
            roundLabel.text = "Round " + round;

        if (timeLabel != null)
        {
            int min = Mathf.FloorToInt(t / 60f);
            int sec = Mathf.FloorToInt(t % 60f);
            timeLabel.text = min + ":" + sec.ToString("D2");
        }
    }
}
