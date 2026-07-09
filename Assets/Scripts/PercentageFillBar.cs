using System.Collections;
using TMPro;
using UnityEngine;

public class PercentageFillBar : FillBar
{
    [SerializeField] TMP_Text _percentage;

    public override void SetFillBarInstant(float rate)
    {
        base.SetFillBarInstant(rate);
        if (_percentage != null)
        {
            _percentage.text = (rate * 100).ToString("F1") + "%";
        }
    }

    protected override IEnumerator LerpFillBar()
    {
        while (_lerpTime > 0)
        {
            _curRate = Mathf.Lerp(_curRate, _targetRate, 10 * Time.deltaTime);
            _fillBar.fillAmount = _curRate;
            _percentage.text = (_curRate * 100).ToString("F1") + "%";

            _lerpTime -= Time.deltaTime;

            yield return null;
        }

        _curRate = _targetRate;
        _fillBar.fillAmount = _curRate;
        _percentage.text = (_curRate * 100).ToString("F1") + "%";
        _lerpTime = 0;
    }
}