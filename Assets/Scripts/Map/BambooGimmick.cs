using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BambooGimmick : MapGimmickBase
{
    [Header("재생 설정")]
    [SerializeField] float stompTime = 0.3f;
    [SerializeField] float regrowTime = 2f;
    [SerializeField] LayerMask surface;

    private Quaternion originalRotation;
    private Coroutine rotateCoroutine;

    [Networked] bool isDown { get; set; }

    List<Player> stepOn = new List<Player>();

    private void OnDrawGizmosSelected()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * 1f;
        Ray ray = new Ray(rayOrigin, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hitInfo, 10, surface))
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(rayOrigin, hitInfo.point);

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(hitInfo.point, 0.1f);
        }
        else
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(rayOrigin, Vector3.down * 10);
        }
    }

    [ContextMenu("RandomGenerate")]
    void RandomGenerate()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * 1f;
        Ray ray = new Ray(rayOrigin, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hitInfo, 10, surface))
        {
            float targetY = hitInfo.point.y - 0.035971f;

            Debug.Log(targetY);
            transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
        }

        Quaternion rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
        transform.rotation = rotation;

        float randomScale = Random.Range(0.6f, 1.2f);
        transform.localScale = new Vector3(randomScale, randomScale, randomScale);
    }

    void Awake()
    {
        originalRotation = transform.localRotation;
        isDown = false;
    }

    protected override void OnPlayerEnter(Player player)
    {
        if (!Object.HasStateAuthority) return;
        if (stepOn.Contains(player)) return;

        stepOn.Add(player);

        if (isDown) return;

        Vector3 entryDirection = (transform.position - player.transform.position).normalized;
        entryDirection.y = 0;

        // 갈대의 로컬 기준 밀려날 회전 축(Cross Product) 또는 방향 각도 산출
        Vector3 localDir = transform.InverseTransformDirection(entryDirection);

        // Z축/X축 중 꺾일 기준 축 설정 (여기서는 진입 방향 반대로 기울어지도록 계산)
        Vector3 bendAxis = Vector3.Cross(Vector3.up, localDir).normalized;
        Quaternion targetRotation = originalRotation * Quaternion.AngleAxis(85f, bendAxis);

        CheckCoroutineCollide(targetRotation, stompTime);
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        if (stepOn.Count == 0 && isDown)
            CheckCoroutineCollide(originalRotation, regrowTime);
    }

    protected override void OnPlayerExit(Player player)
    {
        if (!Object.HasStateAuthority) return;

        stepOn.Remove(player);
    }

    void CheckCoroutineCollide(Quaternion targetRot, float duration)
    {
        if (rotateCoroutine != null)
            return;

        Rpc_BroadcastBend(targetRot, duration);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void Rpc_BroadcastBend(Quaternion targetRot, float duration)
    {
        rotateCoroutine = StartCoroutine(AnimateRotation(targetRot, duration));

        if (targetRot == originalRotation)
            isDown = false;
        else
            isDown = true;
    }

    IEnumerator AnimateRotation(Quaternion targetRot, float duration)
    {
        Quaternion startRot = transform.localRotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // EaseOut/EaseIn 효과를 주려면 SmoothStep 활용 가능
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            transform.localRotation = Quaternion.Slerp(startRot, targetRot, smoothT);
            yield return null;
        }

        transform.localRotation = targetRot;
        rotateCoroutine = null;
    }
}
