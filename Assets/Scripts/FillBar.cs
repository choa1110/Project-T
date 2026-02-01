using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FillBar : MonoBehaviour
{
    [SerializeField] protected Image _fillBar;

    protected float _targetRate;
    protected float _curRate = 1f;
    protected float _lerpTime;

    public virtual void UpdateFillBar(float rate)
    {
        _targetRate = rate;

        if (_lerpTime <= 0)
        {
            _lerpTime = 0.3f;
            StartCoroutine(LerpFillBar());
        }
        else
            _lerpTime = 0.3f;
    }

    protected virtual IEnumerator LerpFillBar()
    {
        while (_lerpTime > 0)
        {
            _curRate = Mathf.Lerp(_curRate, _targetRate, 5 * Time.deltaTime);
            _fillBar.fillAmount = _curRate;

            _lerpTime -= Time.deltaTime;

            yield return null;
        }

        _curRate = _targetRate;
        _fillBar.fillAmount = _curRate;
        _lerpTime = 0;
        
        // 1. A에서 B까지 부드럽게 전환
        // A = lerp(A, B, delta * 속도)

        // 2. A에서 B까지 일정 시간 내에 전환
        // A = lerp(s, e, 0~1)
    }
}