using System.Collections;
using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    public Player target;

    [SerializeField] float camDis;
    [SerializeField] float angleX;
    [SerializeField] private LayerMask transLayer;

    Vector3 _movePos;
    Vector3 _offset = new Vector3(0, 0, 0);

    void Start()
    {
        Vector3 followPos = target.transform.position;
        followPos.y += 1.2f;

        transform.forward = followPos - transform.position;
    }

    void LateUpdate()
    {
        Vector3 followPos = target.transform.position;
        followPos.y += 1.2f;

        Vector3 dir = Quaternion.Euler(angleX, 0f, 0f) * Vector3.forward;

        RaycastHit[] hits = Physics.RaycastAll(followPos, dir, camDis, transLayer);

        foreach (RaycastHit hit in hits)
        {
            // 시야를 가리는 장애물 투명화 (구현 예정) 
            Debug.Log(hit.collider.name);
        }

        _movePos = target.transform.position + dir * camDis;

        transform.position = _movePos + _offset;
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
}
