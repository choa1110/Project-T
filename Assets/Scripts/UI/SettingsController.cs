using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 설정 적용/저장. AudioMixer가 없어 마스터 볼륨만 즉시 적용(AudioListener),
/// BGM/SFX·닉네임은 PlayerPrefs에 저장(사운드매니저/네트워크 이름에서 읽어 사용).
/// </summary>
public class SettingsController : MonoBehaviour
{
    [SerializeField] Slider masterSlider;
    [SerializeField] Slider bgmSlider;
    [SerializeField] Slider sfxSlider;
    [SerializeField] Toggle fullscreenToggle;
    [SerializeField] TMP_Dropdown qualityDropdown;
    [SerializeField] TMP_InputField nicknameInput;

    public const string K_Master = "opt_master";
    public const string K_Bgm = "opt_bgm";
    public const string K_Sfx = "opt_sfx";
    public const string K_Quality = "opt_quality";
    public const string K_Nick = "PlayerNickname";

    void Awake()
    {
        // 패널을 한 번도 안 열어도 시작 시 저장값 적용
        AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(K_Master, 1f));
        if (PlayerPrefs.HasKey(K_Quality))
            QualitySettings.SetQualityLevel(Mathf.Clamp(PlayerPrefs.GetInt(K_Quality), 0, QualitySettings.names.Length - 1), true);
    }

    void OnEnable()
    {
        if (masterSlider) masterSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(K_Master, 1f));
        if (bgmSlider) bgmSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(K_Bgm, 1f));
        if (sfxSlider) sfxSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(K_Sfx, 1f));
        if (fullscreenToggle) fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
        if (qualityDropdown)
        {
            qualityDropdown.ClearOptions();
            qualityDropdown.AddOptions(new List<string>(QualitySettings.names));
            qualityDropdown.SetValueWithoutNotify(QualitySettings.GetQualityLevel());
            qualityDropdown.RefreshShownValue();
        }
        if (nicknameInput) nicknameInput.SetTextWithoutNotify(PlayerPrefs.GetString(K_Nick, ""));
    }

    public void OnMaster(float v) { AudioListener.volume = Mathf.Clamp01(v); PlayerPrefs.SetFloat(K_Master, v); }
    public void OnBgm(float v) { PlayerPrefs.SetFloat(K_Bgm, v); }
    public void OnSfx(float v) { PlayerPrefs.SetFloat(K_Sfx, v); }
    public void OnFullscreen(bool on) { Screen.fullScreen = on; }
    public void OnQuality(int idx) { QualitySettings.SetQualityLevel(idx, true); PlayerPrefs.SetInt(K_Quality, idx); }
    public void OnNickname(string s) { PlayerPrefs.SetString(K_Nick, s); }
}
