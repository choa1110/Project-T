using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    CharacterController _controller;
    Animator _anim;
    InputSystem _input;
    CharacterInfo _info;

    public FollowCamera POV;
    public AttackParameters paramlist;

    [SerializeField] int _modelNum;
    public int team;
    public PlayerStats stats;
    public int life;
    public int jumpAbiliy;

    public List<GameObject> modelList;

    public UnityEvent onHit;

    Vector3 _horVelocity;
    float _verVelocity;
    float _blendSpeedY;
    float _blendSpeedX;

    bool _isDead;
    bool _isGrounded = true;
    bool _isMoveable = true;
    bool _jumpedThisFrame;
    float _jumpStartTimer;

    int _jumpCount;

    float _curHP;
    int _curLife;

    List<AttackArea> attackAreas = new List<AttackArea>();

    Coroutine _knockbackCor;

    [SerializeField] private LayerMask _groundLayer;

    const float Gravity = -9.81f;
    const float SticktoGround = -2f;
    const float GroundTurnLerp = 5f;
    const float AirTurnLerp = 1.5f;

    const float sphereRadius = 0.3f;
    const float castDistance = 0.3f;
    const float rayOffsetY = 0.3f;

    void OnDrawGizmos()
    {
        Vector3 origin = transform.position + Vector3.up * rayOffsetY;
        Vector3 direction = Vector3.down;

        Ray groundRay = new Ray(origin, direction);

        bool hit = Physics.SphereCast(
            groundRay,
            sphereRadius,
            castDistance,
            _groundLayer
        );

        // 시작 Sphere
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(origin, sphereRadius);

        // 히트 여부에 따라 색상 변경
        Gizmos.color = hit ? Color.green : Color.red;

        // 끝 Sphere
        Vector3 endPos = origin + direction * castDistance;
        Gizmos.DrawWireSphere(endPos, sphereRadius);

        // 중심 Ray
        Gizmos.DrawLine(origin, endPos);
    }

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _anim = GetComponent<Animator>();
        _input = GetComponent<InputSystem>();

        modelList[_modelNum].SetActive(true);

        _info = modelList[_modelNum].GetComponent<CharacterInfo>();
        _anim.avatar = _info.avatar;

        foreach (AttackArea area in _info.fists)
        {
            attackAreas.Add(area);
            area.SetOwner(gameObject);
        }

        stats.InitalizeStats();
    }

    public void RoundStart()
    {
        _curHP = stats.GetStat(StatType.MaxHP).Value;
        _curLife = life;
        _jumpCount = jumpAbiliy;
    }

    void Update()
    {
        _anim.SetFloat("MoveSpeedRate", stats.GetStatRate(StatType.SpeedMove));
        _anim.SetFloat("AtkSpeedRate", stats.GetStatRate(StatType.AtkSpeed));
        _jumpStartTimer = Mathf.Max(0, _jumpStartTimer - Time.deltaTime);

        GroundCheck();
        ApplyGravity();

        if (!_isDead)
        {
            if (_isMoveable)
            {
                Movement();
                Rotation();
            }
            else
            {
                _horVelocity = Vector3.zero;
                _blendSpeedY = 0;
                _blendSpeedX = 0;
                _anim.SetFloat("SpeedX", _blendSpeedX);
                _anim.SetFloat("SpeedY", _blendSpeedY);
            }

            Combo();
        }

        if (Keyboard.current.hKey.wasPressedThisFrame)
        {
            // 테스트용
            ApplyHit(transform.position, 1, new Vector3(1, 0f, 1), 20, 10);
        }

        _controller.Move((_horVelocity + Vector3.up * _verVelocity) * Time.deltaTime);
    }

    void GroundCheck()
    {
        Ray groundRay = new Ray(transform.position + Vector3.up * 0.3f, Vector3.down);

        bool hit = Physics.SphereCast(groundRay, 0.3f, 0.3f, _groundLayer);

        if (hit && _verVelocity <= 0)
        {
            _isGrounded = true;
            _jumpCount = jumpAbiliy;
        }
        else
            _isGrounded = false;

        _anim.SetBool("Airborne", !_isGrounded);
    }

    void ApplyGravity()
    {
        if (!_isGrounded)
            _verVelocity += Gravity * Time.deltaTime;
        else if (_jumpStartTimer <= 0)
            _verVelocity = SticktoGround;
    }

    void Movement()
    {
        Vector3 inputDir = POV.transform.right * _input.move.x + POV.transform.forward * _input.move.y;
        inputDir.y = 0f;

        _blendSpeedY = Mathf.Lerp(_blendSpeedY, Sign(_input.move.y), 5f * Time.deltaTime);
        _blendSpeedX = Mathf.Lerp(_blendSpeedX, Sign(_input.move.x), 5f * Time.deltaTime);
        _anim.SetFloat("SpeedX", _blendSpeedX);
        _anim.SetFloat("SpeedY", _blendSpeedY);

        if (_input.jump && !_jumpedThisFrame && _jumpCount > 0)
        {
            _jumpStartTimer = 0.15f;
            _jumpCount--;
            _verVelocity = Mathf.Sqrt(2f * -Gravity * stats.GetStat(StatType.JumpHeight).Value);

            _anim.SetTrigger("Jump");
        }

        if (_isGrounded)
            _horVelocity = stats.GetStat(StatType.SpeedMove).Value * inputDir.normalized;
        else
            _horVelocity = stats.GetStat(StatType.SpeedMove).Value * 0.7f * inputDir.normalized;

        _jumpedThisFrame = _input.jump;
    }

    void Rotation()
    {
        if (_input.move == Vector2.zero)
            return;

        float moveAngle = Mathf.Atan2(_input.move.x, _input.move.y) * Mathf.Rad2Deg + POV.transform.eulerAngles.y;
        Quaternion targetRot = Quaternion.Euler(0f, moveAngle, 0f);
        float t = _isGrounded ? GroundTurnLerp : AirTurnLerp;

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, t * Time.deltaTime);
    }

    void Combo()
    {
        if (_input.attack && _isGrounded)
        {
            _anim.SetBool("Combo", true);
        }
    }

    public void ApplyHit(Vector3 pos, float damage, Vector3 knockDir, float knockPow, float camShake)
    {
        if (_isDead) return;

        _curHP = Mathf.Clamp(_curHP - damage, 0, stats.GetStat(StatType.MaxHP).Value);
        onHit.Invoke();

        if (!_isGrounded)
            knockPow *= 1.5f;

        Vector3 kbDir = knockDir.normalized;
        float knockDis = knockPow / Mathf.Max(0.1f, stats.GetStat(StatType.Weight).Value);
        Vector3 initialVel = kbDir * knockDis;

        Knockback(initialVel);

        SetHit(pos, knockDis);

        POV.CameraShake(camShake);
    }


    public void Knockback(Vector3 initialVel)
    {
        if (_knockbackCor != null)
            StopCoroutine(_knockbackCor);

        _knockbackCor = StartCoroutine(KnockbackRoutine(initialVel));
    }

    static float Sign(float v)
    {
        if (v < 0f) return -1f;
        if (v > 0f) return 1f;
        return 0f;
    }

    // Animation Events
    public void EnableMovement() { _isMoveable = true; }
    public void DisableMovement() { _isMoveable = false; }

    public void InAttack(int num)
    {
        attackAreas[num].AttackStart();
    }

    public void OutAttack(int num)
    {
        attackAreas[num].AttackEnd();
    }

    public void SetAttackStats(int num)
    {
        float dam = stats.GetStat(StatType.PowDam).Value;
        float knock = stats.GetStat(StatType.PowKnock).Value;

        foreach (AttackArea area in attackAreas)
            area.SetAttackStatus(paramlist.parameters[num], dam, knock);
    }

    public void ResetCombo()
    {
        _anim.SetBool("Combo", false);
    }

    void SetHit(Vector3 hitPoint, float hitDis)
    {
        Vector3 velocity = _controller.velocity;
        velocity.y = 0;

        if (Vector3.Angle(transform.forward, hitPoint - transform.position) > 90)
        {
            if (hitDis > 20f)
                _anim.SetInteger("Hit", 4);
            else
                _anim.SetInteger("Hit", 2);
        }
        else
        {
            if (hitDis > 20f)
                _anim.SetInteger("Hit", 3);
            else
                _anim.SetInteger("Hit", 1);

            velocity *= -1;
        }

        if (velocity.sqrMagnitude > 0.001f)
            StartCoroutine(RotateByVelocity(velocity));
    }

    public void ResetHit()
    {
        _anim.SetInteger("Hit", 0);
    }

    public void SetKnockedHit()
    {
        _anim.SetInteger("Hit", 5);
    }

    // Coroutines
    IEnumerator KnockbackRoutine(Vector3 initialVel)
    {
        Vector3 vel = initialVel;
        Vector3 minVel = initialVel * 0.2f;
        float time = 0f;

        while (time < 0.7f || !_isGrounded)
        {
            vel = Vector3.Lerp(vel, minVel, 5f * Time.deltaTime);

            Vector3 delta = vel * Time.deltaTime;

            _controller.Move(delta);

            time += Time.deltaTime;

            yield return null;
        }

        _knockbackCor = null;
    }

    IEnumerator RotateByVelocity(Vector3 tarVel)
    {
        Quaternion targetRotation = Quaternion.LookRotation(tarVel);
        float time = 0f;

        while (time <= 0.3f)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, time * 3.3f);
            time += Time.deltaTime;

            yield return null;
        }
    }
}