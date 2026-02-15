using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Fusion;

public class Player : NetworkBehaviour
{
    CharacterController _controller;
    Animator _anim;
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

    // 이동 관련 변수
    Vector3 _horVelocity;
    float _verVelocity;
    float _blendSpeedY;
    float _blendSpeedX;

    // 상태 변수
    [Networked] public bool IsDead { get; set; } // 동기화 필요
    bool _isGrounded = true;
    bool _isMoveable = true;
    float _jumpStartTimer;
    int _jumpCount;

    // 체력은 중요하므로 Networked로 관리 (간단한 구현)
    [Networked] public float CurrentHP { get; set; }
    int _curLife;

    List<AttackArea> attackAreas = new List<AttackArea>();

    // 넉백 처리를 위한 변수 (Coroutine 대신 사용)
    Vector3 _currentKnockbackVelocity;
    float _knockbackTimer;

    [SerializeField] private LayerMask _groundLayer;

    const float Gravity = -9.81f;
    const float SticktoGround = -2f;
    const float GroundTurnLerp = 5f;
    const float AirTurnLerp = 1.5f;

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _anim = GetComponent<Animator>();

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

    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            POV = FindFirstObjectByType<FollowCamera>();
            POV.target = this;
        }
        RoundStart();
    }

    public void RoundStart()
    {
        CurrentHP = stats.GetStat(StatType.MaxHP).Value; // _curHP -> CurrentHP
        _curLife = life;
        _jumpCount = jumpAbiliy;
    }

    // Update는 오직 "애니메이션 보간"이나 "UI 갱신"용입니다. 로직은 넣지 마세요.
    void Update()
    {
        _anim.SetFloat("MoveSpeedRate", stats.GetStatRate(StatType.SpeedMove));
        _anim.SetFloat("AtkSpeedRate", stats.GetStatRate(StatType.AtkSpeed));
    }

    // ★★★ 핵심 물리/이동 로직 ★★★
    public override void FixedUpdateNetwork()
    {
        // 1. 타이머 갱신 (Time.deltaTime 대신 Runner.DeltaTime)
        _jumpStartTimer = Mathf.Max(0, _jumpStartTimer - Runner.DeltaTime);
        
        // 2. 물리 체크
        GroundCheck();
        ApplyGravity();
        ProcessKnockback(); // 넉백 계산

        // 3. 입력 처리
        if (GetInput(out NetworkInputData data))
        {
            if (!IsDead)
            {
                if (_isMoveable)
                {
                    Movement(data);
                    Rotation(data);
                    
                    if (data.buttons.IsSet(InputButtons.Attack)) 
                    {
                        Combo();
                    }
                }
                else
                {
                    _horVelocity = Vector3.zero;
                    _blendSpeedY = 0;
                    _blendSpeedX = 0;
                    // 애니메이터 설정은 필요 시 여기에 유지
                }
            }
        }

        // 5. 최종 이동 적용 (이동 속도 + 수직 속도 + 넉백 속도)
        Vector3 finalVelocity = _horVelocity + (Vector3.up * _verVelocity) + _currentKnockbackVelocity;
        _controller.Move(finalVelocity * Runner.DeltaTime);
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
            _verVelocity += Gravity * Runner.DeltaTime;
        else if (_jumpStartTimer <= 0)
            _verVelocity = SticktoGround;
    }

    void Movement(NetworkInputData data)
    {
        Vector3 inputDir;

        if (POV != null)
            inputDir = POV.transform.right * data.direction.x + POV.transform.forward * data.direction.y;
        else
            inputDir = new Vector3(data.direction.x, 0, data.direction.y);

        inputDir.y = 0f;

        _blendSpeedY = Mathf.Lerp(_blendSpeedY, Sign(data.direction.y), 5f * Runner.DeltaTime);
        _blendSpeedX = Mathf.Lerp(_blendSpeedX, Sign(data.direction.x), 5f * Runner.DeltaTime);
        _anim.SetFloat("SpeedX", _blendSpeedX);
        _anim.SetFloat("SpeedY", _blendSpeedY);

        if (_isGrounded)
            _horVelocity = stats.GetStat(StatType.SpeedMove).Value * inputDir.normalized;
        else
            _horVelocity = stats.GetStat(StatType.SpeedMove).Value * 0.7f * inputDir.normalized;
    }

    void Rotation(NetworkInputData data)
    {
        if (data.direction == Vector2.zero)
            return;

        float cameraY = POV != null ? POV.transform.eulerAngles.y : 0;
        float moveAngle = Mathf.Atan2(data.direction.x, data.direction.y) * Mathf.Rad2Deg + cameraY;

        Quaternion targetRot = Quaternion.Euler(0f, moveAngle, 0f);
        float t = _isGrounded ? GroundTurnLerp : AirTurnLerp;

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, t * Runner.DeltaTime);
    }

    // ★ 공격 입력 처리
    void Combo()
    {
        if (_isGrounded)
        {
            _anim.SetBool("Combo", true);
        }
    }

    // ★ 피격 처리 (중복된 함수 하나로 통합 및 수정)
    public void ApplyHit(Vector3 pos, float damage, Vector3 knockDir, float knockPow, float camShake)
    {
        if (IsDead) return;

        // 체력 감소 (서버 권한 필요)
        CurrentHP = Mathf.Clamp(CurrentHP - damage, 0, stats.GetStat(StatType.MaxHP).Value);
        onHit.Invoke();

        if (!_isGrounded)
            knockPow *= 1.5f;

        Vector3 kbDir = knockDir.normalized;
        float knockDis = knockPow / Mathf.Max(0.1f, stats.GetStat(StatType.Weight).Value);
        Vector3 initialVel = kbDir * knockDis;

        // 코루틴 대신 함수 호출
        StartKnockback(initialVel);
        SetHit(pos, knockDis);

        if(Object.HasInputAuthority && POV != null) 
            POV.CameraShake(camShake);
    }

    // ★ 넉백 시작 (코루틴 대체)
    void StartKnockback(Vector3 initialVel)
    {
        _currentKnockbackVelocity = initialVel;
        _knockbackTimer = 0.7f; // 넉백 지속 시간
    }

    // ★ 매 프레임(Tick)마다 넉백 속도를 줄임
    void ProcessKnockback()
    {
        if (_knockbackTimer > 0)
        {
            // 속도 감쇠 (Lerp 사용)
            Vector3 minVel = _currentKnockbackVelocity * 0.2f;
            _currentKnockbackVelocity = Vector3.Lerp(_currentKnockbackVelocity, minVel, 5f * Runner.DeltaTime);
            
            _knockbackTimer -= Runner.DeltaTime;
        }
        else
        {
            _currentKnockbackVelocity = Vector3.zero;
        }
    }

    // 유틸리티
    static float Sign(float v)
    {
        if (v < 0f) return -1f;
        if (v > 0f) return 1f;
        return 0f;
    }

    // Animation Events 및 기타 함수들
    public void EnableMovement() { _isMoveable = true; }
    public void DisableMovement() { _isMoveable = false; }
    public void InAttack(int num) { attackAreas[num].AttackStart(); }
    public void OutAttack(int num) { attackAreas[num].AttackEnd(); }
    
    public void SetAttackStats(int num)
    {
        float dam = stats.GetStat(StatType.PowDam).Value;
        float knock = stats.GetStat(StatType.PowKnock).Value;
        foreach (AttackArea area in attackAreas)
            area.SetAttackStatus(paramlist.parameters[num], dam, knock);
    }

    public void ResetCombo() { _anim.SetBool("Combo", false); }

    void SetHit(Vector3 hitPoint, float hitDis)
    {
        // (피격 애니메이션 로직 유지)
        if (hitDis > 20f) _anim.SetInteger("Hit", 3); // 예시 단순화
        else _anim.SetInteger("Hit", 1);
        
        // 회전 로직이 필요하다면 여기서 즉시 transform.LookAt 등을 사용하거나
        // FixedUpdateNetwork에서 회전 보간을 처리해야 합니다.
        // 코루틴 RotateByVelocity는 삭제하는 게 좋습니다.
    }

    public void ResetHit() { _anim.SetInteger("Hit", 0); }
    public void SetKnockedHit() { _anim.SetInteger("Hit", 5); }
}