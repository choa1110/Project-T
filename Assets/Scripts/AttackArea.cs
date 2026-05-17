using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class AttackArea : NetworkBehaviour
{
    GameObject _owner;
    Player _playerControl;
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

    public void SetOwner(GameObject player)
    {
        _owner = player;

        _playerControl = _owner.GetComponent<Player>();
    }

    public void SetAttackStatus(AttackParameter input, float damage, float knock)
    {
        _param = input;
        _damPow = damage * _param.DamageRate;
        _knockPow = knock * _param.KnockbackRate;
    }

    void FixedUpdate()
    {
        if(_owner == null || !_inAttack) return;

        if (_playerControl != null)
        {
            if (!_playerControl.Object || !_playerControl.Object.HasInputAuthority)
                return;
        }

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
        if (target && target != _playerControl && !_hit_objs.Contains(target))
        {
            // 팀 킬 방지 로직인데 2vs2 만들 예정이므로 주석처리
            // if(_ownerPlayer.team == target.team)
            //     return;

            _hit_objs.Add(target);

            Vector3 hitPos = _owner.transform.position;
            hitPos.y += 1f;

            Vector3 tmpz = _owner.transform.forward * _param.KnockbackDir.z;
            Vector3 tmpx = _owner.transform.right * _param.KnockbackDir.x;
            Vector3 tmpy = _owner.transform.up * _param.KnockbackDir.y;

            Vector3 knockDir = tmpz + tmpx + tmpy;

            if (Object.HasInputAuthority)
            {
                Rpc_RequestHitToServer(
                    target.Object,
                    _playerControl.Object,
                    hitPos,
                    _damPow,
                    knockDir,
                    _knockPow,
                    _param.CameraShake
                );
            }
            else if (Object.HasStateAuthority)
            {
                target.ApplyHit(_playerControl, hitPos, _damPow, knockDir, _knockPow, _param.CameraShake);
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void Rpc_RequestHitToServer(NetworkObject targetObject, NetworkObject attackerObject, Vector3 hitPos, float damage, Vector3 knockDir, float knockPow, float camShake)
    {
        if (targetObject == null) return;

        Player targetPlayer = targetObject.GetComponent<Player>();
        Player attackerPlayer = attackerObject != null ? attackerObject.GetComponent<Player>() : null;

        if (targetPlayer != null)
            targetPlayer.ApplyHit(attackerPlayer, hitPos, damage, knockDir, knockPow, camShake);
    }
}