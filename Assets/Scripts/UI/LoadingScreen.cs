using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 씬 전환 로딩 화면. 가운데 회전 애니메이션 + 하단 로딩 바.
/// 싱글톤으로 동작하며 DontDestroyOnLoad로 씬 전환 간 유지된다.
///
/// 사용:
///   LoadingScreen.Instance.Show();
///   LoadingScreen.Instance.SetProgress(0.5f);
///   LoadingScreen.Instance.Hide();
/// </summary>
public class LoadingScreen : MonoBehaviour
{
    public static LoadingScreen Instance { get; private set; }

    [Header("루트 / 페이드")]
    public CanvasGroup canvasGroup;     // 전체 페이드용
    public float fadeDuration = 0.35f;

    [Header("가운데 회전 애니메이션")]
    public RectTransform spinner;       // 회전시킬 오브젝트 (우주선/행성)
    public float spinSpeed = 180f;      // 초당 회전 각도

    [Header("하단 로딩 바")]
    public Image progressFill;          // fillAmount로 진행 표시
    public TMP_Text progressText;       // % 텍스트 (선택)
    public TMP_Text tipText;            // 하단 팁/설명 (선택)

    [Header("진행 옵션")]
    public bool autoFakeProgress = true; // 실제 진행률 없을 때 부드럽게 차오르게
    public float fakeSpeed = 0.4f;

    bool _isShowing;
    float _targetProgress;
    float _displayProgress;
    Coroutine _fadeRoutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
        gameObject.SetActive(true);
    }

    void Update()
    {
        if (!_isShowing) return;

        // 가운데 스피너 회전
        if (spinner != null)
            spinner.Rotate(0f, 0f, -spinSpeed * Time.unscaledDeltaTime);

        // 진행 바 부드럽게 보간
        float goal = _targetProgress;
        if (autoFakeProgress && _targetProgress <= 0f)
            goal = Mathf.Min(0.9f, _displayProgress + fakeSpeed * Time.unscaledDeltaTime);

        _displayProgress = Mathf.MoveTowards(_displayProgress, goal, fakeSpeed * Time.unscaledDeltaTime);

        if (progressFill != null) progressFill.fillAmount = _displayProgress;
        if (progressText != null) progressText.text = Mathf.RoundToInt(_displayProgress * 100f) + "%";
    }

    public void Show()
    {
        _isShowing = true;
        _displayProgress = 0f;
        _targetProgress = 0f;
        if (progressFill != null) progressFill.fillAmount = 0f;

        StartFade(1f, true);
    }

    public void Hide()
    {
        // 완료 후 페이드 아웃
        _targetProgress = 1f;
        StartFade(0f, false);
    }

    public void SetProgress(float value01)
    {
        _targetProgress = Mathf.Clamp01(value01);
    }

    public void SetTip(string tip)
    {
        if (tipText != null) tipText.text = tip;
    }

    void StartFade(float targetAlpha, bool show)
    {
        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha, show));
    }

    IEnumerator FadeRoutine(float targetAlpha, bool show)
    {
        if (canvasGroup == null) yield break;

        if (show)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        float start = canvasGroup.alpha;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, targetAlpha, t / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = targetAlpha;

        if (!show)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            _isShowing = false;
        }
    }
}
