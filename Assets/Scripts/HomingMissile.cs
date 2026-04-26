using Fusion;
using UnityEngine;

public class HomingMissile : NetworkBehaviour
{
    NetworkObject _networkObj;
    Rigidbody _rb;
    AttackArea _attackArea;

    [Networked] Player _target { get; set; }
    [Networked] public Vector3 NetworkedVelocity { get; set; }

    public AttackParameters param;
    public float speed;
    public float turnSpeed;

    void Awake()
    {
        _networkObj = GetComponent<NetworkObject>();
        _rb = GetComponent<Rigidbody>();
        _attackArea = GetComponent<AttackArea>();
    }

    public void SetTarget(Player player)
    {
        _target = player;

        _attackArea.SetOwner(gameObject);
        _attackArea.SetAttackStatus(param.parameters[0], 10, 20);
        _attackArea.AttackStart();
    }

    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority)
        {
            // 서버에서 물리 계산 후 변수에 저장
            NetworkedVelocity = _rb.linearVelocity;
        }
        else
        {
            // 클라이언트는 서버가 보낸 속도값을 적용
            _rb.linearVelocity = NetworkedVelocity;
        }

        if (!Object.HasStateAuthority) return;
        if (_target == null) return;

        Vector3 flyPos = _target.transform.position;
        flyPos.y += 1f;

        Vector3 dir = (flyPos - transform.position).normalized;

        Quaternion targetRot = Quaternion.LookRotation(dir);

        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Runner.DeltaTime);
        _rb.linearVelocity = transform.forward * speed;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Object.HasStateAuthority)
            Runner.Despawn(_networkObj);
    }
}