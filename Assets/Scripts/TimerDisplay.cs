using TMPro;
using UnityEngine;

public class TimerDisplay : MonoBehaviour
{
    TMP_Text display;

    public float initTime;

    void Awake()
    {
        display = GetComponent<TMP_Text>();
    }
     
    // 테스트용 -> 서버 호출
    void Update()
    {
        initTime -= Time.deltaTime;
        UpdateDisplay(initTime);
    }

    public void UpdateDisplay(float remainingTime)
    {
        int minutes = Mathf.FloorToInt(remainingTime / 60f);
        int seconds = Mathf.FloorToInt(remainingTime % 60f);

        display.text = $"{minutes:0}:{seconds:00}";
    }
}