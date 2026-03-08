using TMPro;
using UnityEngine;

public class TimerDisplay : MonoBehaviour
{
    TMP_Text display;


    void Awake()
    {
        display = GetComponent<TMP_Text>();
    }
     
    void Update()
    {
        if(GameManager.instance != null && GameManager.instance.Object != null && GameManager.instance.Object.IsValid)
        {
            UpdateDisplay(GameManager.instance.RoundTimer);
        }
        else
        {
            UpdateDisplay(10f); //일단은 10초
        }
    }

    public void UpdateDisplay(float remainingTime)
    {
        remainingTime = Mathf.Max(0, remainingTime);

        int minutes = Mathf.FloorToInt(remainingTime / 60f);
        int seconds = Mathf.FloorToInt(remainingTime % 60f);

        display.text = $"{minutes:0}:{seconds:00}";
    }
}