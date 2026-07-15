using UnityEngine;
using Fusion;
using UnityEngine.Rendering.Universal;

public class HomingMissile : NetworkBehaviour
{
    [SerializeField] NetworkObject _networkObj;
    [SerializeField] Collider _col;
    [SerializeField] Rigidbody _rb;

    [Networked] Player _target { get; set; }
    [Networked] Player _owner { get; set; }

    public AttackParameters param;
    AttackParameter _param;

    [SerializeField] float _speed;
    [SerializeField] float _turnSpeed;
    [SerializeField] float _areaRadius;
    [SerializeField] float _damPow;
    [SerializeField] float _knockPow;

    [SerializeField] LayerMask _attackLayer;

    [SerializeField] GameObject _body;
    [SerializeField] ParticleSystem _explosion;
    [SerializeField] AudioSource _boom;

    [Networked] TickTimer despawnTimer { get; set; }

    bool _exploded = false;

    void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, _areaRadius);
    }

    public void SetOwner(Player player)
    {
        _owner = player;
        _param = param.parameters[0];
    }

    public void SetTarget(Player player)
    {
        _target = player;
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority || _target == null) return;

        if (despawnTimer.Expired(Runner))
        {
            Runner.Despawn(_networkObj);
            return;
        }

        Vector3 flyPos = _target.transform.position;
        flyPos.y += 1f;

        Vector3 dir = (flyPos - transform.position).normalized;

        Quaternion targetRot = Quaternion.LookRotation(dir);

        if (!_exploded)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, _turnSpeed * Runner.DeltaTime);
            _rb.linearVelocity = transform.forward * _speed;
        }
        else
            _rb.linearVelocity = Vector3.zero;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!Object.HasStateAuthority) return;

        Collider[] overlaps = Physics.OverlapSphere(transform.position, _areaRadius, _attackLayer);

        foreach (Collider collider in overlaps)
        {
            if (collider.transform.TryGetComponent<Player>(out Player target))
                CheckCollides(target);
        }

        Rpc_BroadcastExplode();

        despawnTimer = TickTimer.CreateFromSeconds(Runner, 1.5f);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void Rpc_BroadcastExplode()
    {
        _body.SetActive(false);
        _col.enabled = false;

        _explosion.Play();
        _boom.Play();
        _exploded = true;
    }

    void CheckCollides(Player target)
    {
        Vector3 targetDir = (target.transform.position - transform.position).normalized;
        Quaternion rotation = Quaternion.LookRotation(targetDir);

        Vector3 tmpz = targetDir * _param.KnockbackDir.z;
        Vector3 tmpx = rotation * Vector3.right * _param.KnockbackDir.x;
        Vector3 tmpy = rotation * Vector3.up * _param.KnockbackDir.y;

        Vector3 knockDir = tmpz + tmpx + tmpy;
        target.ApplyHit(_owner, transform.position, _damPow, knockDir, _knockPow, _param.CameraShake);
    }
}