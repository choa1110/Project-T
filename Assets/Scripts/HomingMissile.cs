using UnityEngine;

public class HomingMissile : MonoBehaviour
{
    Rigidbody _rb;
    AttackArea _attackArea;

    Player _target;

    public AttackParameters param;
    public float speed;
    public float turnSpeed;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _attackArea = GetComponent<AttackArea>();
    }

    public void SetTarget(Player player)
    {
        _target = player;

        // _attackArea.SetOwner(gameObject);
        _attackArea.SetAttackStatus(param.parameters[0], 10, 20);
        _attackArea.AttackStart();
    }

    void Update()
    {
        if (_target == null) return;

        Vector3 flyPos = _target.transform.position;
        flyPos.y += 1f;

        Vector3 dir = (flyPos - transform.position).normalized;

        Quaternion targetRot = Quaternion.LookRotation(dir);

        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
        _rb.linearVelocity = transform.forward * speed;
    }

    private void OnTriggerEnter(Collider other)
    {
        Destroy(gameObject);
    }
}