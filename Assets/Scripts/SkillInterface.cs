using UnityEngine;
using UnityEngine.UI;

public class SkillInterface : MonoBehaviour
{
    public Image skillIcon;
    public Image fillBar;

    float coolTime;

    public void SetSkill(Ability ability)
    {
        skillIcon.sprite = ability.icon;
        coolTime = ability.coolTime;

        fillBar.fillAmount = 0.25f;
    }

    public void CoolRate(float time)
    {
        fillBar.fillAmount = time / coolTime / 2;
    }

    public void OnCoolComplete()
    {
        skillIcon.color = new Color32(60, 255, 255, 200);
    }

    public void OnSkillUse()
    {
        skillIcon.color = new Color32(255, 255, 255, 65);
        fillBar.fillAmount = 0;
    }
}