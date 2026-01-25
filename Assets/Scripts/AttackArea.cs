using System.Collections.Generic;
using UnityEngine;

public class AttackArea : MonoBehaviour
{
    GameObject _owner;
    AttackParameter _param;

    [SerializeField] LayerMask _attackLayer;

    public float areaRadius;

    bool _inAttack;
    Vector3 _prevPoint;
    Vector3 _curPoint;

    float _damPow;
    float _knockPow;

    HashSet<Player> _hit_objs = new HashSet<Player>();

    void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, areaRadius);
    }

    public void SetOwner(GameObject obj)
    {
        _owner = obj;
    }

    public void SetAttackStatus(AttackParameter input, float damage, float knock)
    {
        _param = input;
        _damPow = damage * _param.DamageRate;
        _knockPow = knock * _param.KnockbackRate;
    }

    void FixedUpdate()
    {
        if (!_inAttack) return;

        _curPoint = transform.position;

        Vector3 direction = (_prevPoint - _curPoint).normalized;
        float distance = Vector3.Distance(_curPoint, _prevPoint);

        Collider[] overlaps = Physics.OverlapSphere(transform.position, areaRadius, _attackLayer);

        if (overlaps.Length > 0)
        {
            foreach (Collider collider in overlaps)
            {
                Player target = collider.transform.GetComponentInParent<Player>();

                CheckCollides(target);
            }
        }

        if (Physics.SphereCast(_curPoint, areaRadius, direction, out RaycastHit hit, distance, _attackLayer))
        {
            Player target = hit.transform.GetComponentInParent<Player>();

            CheckCollides(target);
        }

        _prevPoint = _curPoint;
    }

    public void AttackStart()
    {
        _hit_objs.Clear();

        _inAttack = true;
        _prevPoint = transform.position;
    }

    public void AttackEnd()
    {
        _inAttack = false;
    }

    void CheckCollides(Player target)
    {
        if (target && target != _owner && !_hit_objs.Contains(target))
        {
            if (_owner.TryGetComponent<Player>(out Player pl))
            {
                if (pl.team == target.team)
                    return;
            }

            _hit_objs.Add(target);

            Vector3 tmpz = _owner.transform.forward * _param.KnockbackDir.z;
            Vector3 tmpx = _owner.transform.right * _param.KnockbackDir.x;
            Vector3 tmpy = _owner.transform.up * _param.KnockbackDir.y;

            target.ApplyHit(_owner.transform.position, _damPow, tmpz + tmpz + tmpy, _knockPow, _param.CameraShake);
        }
    }
}