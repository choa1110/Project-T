using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class AttackArea : NetworkBehaviour
{
    Player _owner;
    AttackParameter _param;

    [SerializeField] LayerMask _attackLayer;
    [SerializeField] float _areaRadius;

    [Networked] bool _inAttack {  get; set; }

    Vector3 _prevPoint;
    Vector3 _curPoint;

    float _damPow;
    float _knockPow;

    HashSet<Player> _hit_objs = new HashSet<Player>();

    void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, _areaRadius);
    }

    public void SetOwner(Player player)
    {
        _owner = player;
    }

    public void SetAttackStatus(AttackParameter input, float damage, float knock)
    {
        _param = input;
        _damPow = damage * _param.DamageRate;
        _knockPow = knock * _param.KnockbackRate;
    }

    void FixedUpdate()
    {
        if (_owner == null || !_inAttack) return;
        if (!Object.HasStateAuthority) return;

        _curPoint = transform.position;

        Vector3 direction = (_prevPoint - _curPoint).normalized;
        float distance = Vector3.Distance(_curPoint, _prevPoint);

        Collider[] overlaps = Physics.OverlapSphere(transform.position, _areaRadius, _attackLayer);

        foreach (Collider collider in overlaps)
        {
            Player target = collider.transform.GetComponentInParent<Player>();
            CheckCollides(target);
        }

        if (Physics.SphereCast(_curPoint, _areaRadius, direction, out RaycastHit hit, distance, _attackLayer))
        {
            Player target = hit.transform.GetComponentInParent<Player>();
            CheckCollides(target);
        }

        _prevPoint = _curPoint;
    }

    public void AttackStart()
    {
        if (Object.HasStateAuthority)
        {
            _hit_objs.Clear();

            _inAttack = true;
            _prevPoint = transform.position;
        }
    }

    public void AttackEnd()
    {
        if (Object.HasStateAuthority)
            _inAttack = false;
    }

    void CheckCollides(Player target)
    {
        if (target && target != _owner && target.team != _owner.team && !_hit_objs.Contains(target))
        {
            _hit_objs.Add(target);

            Vector3 hitPos = _owner.transform.position;
            hitPos.y += 1f;

            Vector3 tmpz = _owner.transform.forward * _param.KnockbackDir.z;
            Vector3 tmpx = _owner.transform.right * _param.KnockbackDir.x;
            Vector3 tmpy = _owner.transform.up * _param.KnockbackDir.y;

            Vector3 knockDir = tmpz + tmpx + tmpy;

            target.ApplyHit(_owner, hitPos, _damPow, knockDir, _knockPow, _param.CameraShake);
        }
    }
}