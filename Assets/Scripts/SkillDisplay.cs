using UnityEngine;
using UnityEngine.UI;

public class SkillDisplay : MonoBehaviour
{
    [SerializeField] Image _skillIcon;
    [SerializeField] Image _cooldownFill;

    public void UpdateCoolGauge(float rate)
    {
        _cooldownFill.fillAmount = rate / 2;

        if (rate >= 100)
        {

        }
    }
}