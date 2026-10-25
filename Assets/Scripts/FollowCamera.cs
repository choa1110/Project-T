using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    static FollowCamera _instance;
    public static FollowCamera Instance { get => _instance; }

    public Player target;

    public List<MaterialTransparent> transRenderers = new();

    [SerializeField] float camDis;
    [SerializeField] float angleX;
    [SerializeField] private LayerMask transLayer;

    Vector3 _movePos;
    Vector3 _armDir;
    Vector3 _offset = new Vector3(0, 0, 0);

    bool inSoftLerp;

    void Awake()
    {
        if (_instance == null)
            _instance = this;
    }

    void Start()
    {
        // ★ [수정 1] 타겟이 없으면 Start 로직을 실행하지 않음 (에러 방지)
        if (target == null) return;

        Vector3 followPos = target.transform.position;
        followPos.y += 1.2f;

        transform.forward = followPos - transform.position;
    }

    void LateUpdate()
    {
        // ★ [수정 2] 플레이어가 아직 스폰되지 않았으면 아무것도 하지 않고 기다림
        // (이 한 줄이 없으면 무한 에러가 발생합니다)
        if (target == null) return;

        // 이전 프레임의 장애물 복구
        foreach (MaterialTransparent mt in transRenderers)
        {
            if (mt != null)
                mt.ToggleTransparency(false);
        }

        transRenderers.Clear();

        // --- 아래는 기존 로직 그대로 ---
        Vector3 followPos = target.transform.position;
        followPos.y += 1.2f;

        _armDir = Quaternion.Euler(angleX, 0f, 0f) * Vector3.forward;

        // 장애물 감지 로직 (Raycast)
        // (필요하다면 플레이어 본인은 제외하도록 LayerMask 설정을 잘 해야 함)
        RaycastHit[] hits = Physics.SphereCastAll(followPos, 0.5f, _armDir, camDis, transLayer);

        foreach (RaycastHit hit in hits)
        {
            MaterialTransparent mt = hit.collider.GetComponentInChildren<MaterialTransparent>();

            if (mt == null)
                continue;

            mt.ToggleTransparency(true);
            
            transRenderers.Add(mt);
        }

        if (inSoftLerp)
            _movePos = Vector3.Lerp(transform.position, target.transform.position + _armDir * camDis, 10 * Time.deltaTime);
        else
            _movePos = target.transform.position + _armDir * camDis;

        transform.position = _movePos + _offset;

        // (참고) 만약 카메라가 항상 플레이어를 바라보게 하려면 아래 코드를 추가하세요.
        // 현재는 angleX로만 방향을 잡고 있습니다.
        // transform.LookAt(target.transform.position + Vector3.up * 1.2f);
    }

    public void ToggleSoft()
    {
        inSoftLerp = true;

        StartCoroutine(LerpTime());
    }

    public void CameraShake(float magnitude)
    {
        StopAllCoroutines();
        StartCoroutine(Shake(magnitude));
    }

    IEnumerator Shake(float magnitude)
    {
        float duration = magnitude / 20f;
        magnitude /= 100f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Mathf.Sin(elapsed * 25f) * magnitude + Random.Range(-0.1f, 0.1f) * magnitude;
            float y = Mathf.Cos(elapsed * 50f) * magnitude + Random.Range(-0.2f, 0.2f) * magnitude;

            _offset = new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        _offset = Vector3.zero;
    }

    IEnumerator LerpTime()
    {
        yield return new WaitForSeconds(0.1f);

        while (Vector3.Distance(transform.position, target.transform.position + _armDir * camDis) > 0.01f)
            yield return null;

        inSoftLerp = false;
    }
}