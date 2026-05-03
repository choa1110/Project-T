using UnityEngine;
using Fusion;

public class HomingMissile : NetworkBehaviour
{
    NetworkObject _networkObj;
    Rigidbody _rb;
    AttackArea _attackArea;

    [Networked] Player _target { get; set; }

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
        if (!Object.HasStateAuthority || _target == null) return;

        Vector3 flyPos = _target.transform.position;
        flyPos.y += 1f;

        Vector3 dir = (flyPos - transform.position).normalized;

        Quaternion targetRot = Quaternion.LookRotation(dir);

        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Runner.DeltaTime);
        _rb.linearVelocity = transform.forward * speed;
    }

    void OnTriggerEnter(Collider other)
    {
        if (Object.HasStateAuthority)
            Runner.Despawn(_networkObj);
    }
}