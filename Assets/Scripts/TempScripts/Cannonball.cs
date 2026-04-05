// using UnityEngine;

// public class Cannonball : MonoBehaviour
// {
//     AttackArea _area;

//     void Awake()
//     {
//         _area = GetComponent<AttackArea>();
//         _area.SetOwner(gameObject);

//         Destroy(gameObject, 5f);
//     }
// }

using UnityEngine;
using Fusion;

public class Cannonball : MonoBehaviour
{
    public float damage = 5f;
    public float knockPow = 25f;

    void Start()
    {
        // 5초 뒤 자동 파괴
        Destroy(gameObject, 5f);
    }

    // 대포알이 무언가에 닿았을 때
    void OnTriggerEnter(Collider other)
    {
        Player target = other.GetComponentInParent<Player>();

        if (target != null && !target.IsDead)
        {
            if (target.HasStateAuthority)
            {
                // 체력 깎기
                target.CurrentHP = Mathf.Clamp(target.CurrentHP - damage, 0, target.stats.GetStat(StatType.MaxHP).Value);

                Vector3 knockDir = transform.forward;
                float knockDis = knockPow / Mathf.Max(0.1f, target.stats.GetStat(StatType.Weight).Value);
                Vector3 initialVel = knockDir.normalized * knockDis;

                // 서버에서 Networked 넉백 변수 셋팅
                //target.StartKnockback(initialVel);

                // 모든 클라이언트에게 피격 이펙트 및 카메라 쉐이크(예: 강도 2f) 실행 명령
                //target.RPC_BroadcastHitEffect(transform.position, knockPow, 2f);
            }

            // 플레이어에 맞았으니 대포알은 즉시 파괴
            Destroy(gameObject);
        }
    }
}