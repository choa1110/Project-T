using UnityEngine;
using Fusion;

public class HomingMissile : NetworkBehaviour
{
    [SerializeField] NetworkObject _networkObj;
    [SerializeField] Rigidbody _rb;

    [Networked] Player _target { get; set; }
    [Networked] Player _owner { get; set; }

    public AttackParameters param;
    AttackParameter _param;

    public float speed;
    public float turnSpeed;
    public float areaRadius;

    [SerializeField] LayerMask _attackLayer;

    [SerializeField] float _damPow;
    [SerializeField] float _knockPow;

    void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, areaRadius);
    }

    void Awake()
    {
        _param = param.parameters[0];
    }

    public void SetTarget(Player player)
    {
        _target = player;
    }

    public void SetOwner(Player player)
    {
        _owner = player;
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
        if (!Object.HasStateAuthority) return;

        Collider[] overlaps = Physics.OverlapSphere(transform.position, areaRadius, _attackLayer);

        if (overlaps.Length > 0)
        {
            foreach (Collider collider in overlaps)
            {
                if (collider.transform.TryGetComponent<Player>(out Player target))
                    CheckCollides(target);
            }
        }

        Runner.Despawn(_networkObj);
    }

    void CheckCollides(Player target)
    {
        if (target == _owner) return;

        Vector3 targetDir = (target.transform.position - transform.position).normalized;
        Quaternion rotation = Quaternion.LookRotation(targetDir);

        Vector3 tmpz = targetDir * _param.KnockbackDir.z;
        Vector3 tmpx = rotation * Vector3.right * _param.KnockbackDir.x;
        Vector3 tmpy = rotation * Vector3.up * _param.KnockbackDir.y;

        Vector3 knockDir = tmpz + tmpx + tmpy;
        target.ApplyHit(_owner, transform.position, _damPow, knockDir, _knockPow, _param.CameraShake);
    }
}