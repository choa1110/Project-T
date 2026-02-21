using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Fusion;

public class Player : NetworkBehaviour
{
    NetworkCharacterController _ncc;
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
    Vector3 _moveDirection;
    float _blendSpeedY;
    float _blendSpeedX;

    // 상태 변수
    [Networked] public bool IsDead { get; set; } // 동기화 필요
    bool _isMoveable = true;
    float _jumpStartTimer;
    int _jumpCount;

    // 체력은 중요하므로 Networked로 관리 (간단한 구현)
    [Networked] public float CurrentHP { get; set; }
    int _curLife;

    List<AttackArea> attackAreas = new List<AttackArea>();

    // Vector3 _currentKnockbackVelocity;
    // float _knockbackTimer;
    [Networked] public Vector3 CurrentKnockbackVelocity { get; set; }
    [Networked] public float KnockbackTimer { get; set; }
    [SerializeField] private LayerMask _groundLayer;

    const float Gravity = -9.81f;
    const float GroundTurnLerp = 5f;
    const float AirTurnLerp = 1.5f;

    void Awake()
    {
        _ncc = GetComponent<NetworkCharacterController>();
        _anim = GetComponent<Animator>();

        modelList[_modelNum].SetActive(true);
        _info = modelList[_modelNum].GetComponent<CharacterInfo>();
        _anim.avatar = _info.avatar;

        foreach (AttackArea area in _info.fists)
        {
            attackAreas.Add(area);
            // area.SetOwner(gameObject);
            area.SetOwner(this);
        }

        stats.InitalizeStats();
    }

    public override void Spawned()
    {
        team = Object.InputAuthority.PlayerId;

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

    // 클라이언트가 호스트에게 때렸다고 요청
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void Rpc_RequestHitToServer(NetworkObject targetObject, Vector3 hitPos, float damage, Vector3 knockDir, float knockPow, float camShake)
    {
        if(targetObject == null) return;

        Player targetPlayer = targetObject.GetComponent<Player>();
        if(targetPlayer != null && !targetPlayer.IsDead)
        {
            targetPlayer.CurrentHP = Mathf.Clamp(targetPlayer.CurrentHP - damage, 0, targetPlayer.stats.GetStat(StatType.MaxHP).Value);


            if(!_ncc.Grounded)
                knockPow *= 1.5f;

            Vector3 kbDir = knockDir.normalized;
            float knockDis = knockPow / Mathf.Max(0.1f, stats.GetStat(StatType.Weight).Value);
            Vector3 initialVel = kbDir * knockDis;

            targetPlayer.StartKnockback(initialVel);
            targetPlayer.RPC_BroadcastHitEffect(hitPos, knockDis, camShake);
        }
    }
    // 서버가 모든 클라이언트에게 정보 전달
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_BroadcastHitEffect(Vector3 hitPos, float knockDis, float camShake)
    {
        onHit.Invoke();

        SetHit(hitPos, knockDis);
        if(Object.HasInputAuthority && POV != null)
            POV.CameraShake(camShake);
    }
    // 핵심 물리/이동 로직 
    public override void FixedUpdateNetwork()
    {
        _jumpStartTimer = Mathf.Max(0, _jumpStartTimer - Runner.DeltaTime);
        ProcessKnockback();

        _moveDirection = Vector3.zero;

        bool isKnockedBack = KnockbackTimer > 0;

        if (GetInput(out NetworkInputData data))
        {
            if (!IsDead && !isKnockedBack)
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
                    _blendSpeedY = 0;
                    _blendSpeedX = 0;
                }
            }
        }

        // NCC 파라미터 설정 및 이동
        float currentSpeed = stats.GetStat(StatType.SpeedMove).Value;
        if (!_ncc.Grounded) currentSpeed *= 0.7f;
        
        _ncc.maxSpeed = currentSpeed;
        _ncc.gravity = Gravity;
        _ncc.acceleration = 100f;

        // 기본 이동 실행
        if (isKnockedBack)
        {
            _ncc.Move(CurrentKnockbackVelocity);
        }
        else
        {
            _ncc.Move(_moveDirection);
        }

        // 애니메이션 및 상태 동기화
        _anim.SetBool("Airborne", !_ncc.Grounded);
        _anim.SetFloat("SpeedX", _blendSpeedX);
        _anim.SetFloat("SpeedY", _blendSpeedY);

        if (_ncc.Grounded)
        {
            _jumpCount = jumpAbiliy;
        }
    }

    void Movement(NetworkInputData data)
    {
        Vector3 inputDir;

        if (POV != null)
            inputDir = POV.transform.right * data.direction.x + POV.transform.forward * data.direction.y;
        else
            inputDir = new Vector3(data.direction.x, 0, data.direction.y);

        inputDir.y = 0f;
        _moveDirection = inputDir.normalized;

        _blendSpeedY = Mathf.Lerp(_blendSpeedY, Sign(data.direction.y), 5f * Runner.DeltaTime);
        _blendSpeedX = Mathf.Lerp(_blendSpeedX, Sign(data.direction.x), 5f * Runner.DeltaTime);
    }

    void Rotation(NetworkInputData data)
    {
        if (data.direction == Vector2.zero)
            return;

        float cameraY = POV != null ? POV.transform.eulerAngles.y : 0;
        float moveAngle = Mathf.Atan2(data.direction.x, data.direction.y) * Mathf.Rad2Deg + cameraY;

        Quaternion targetRot = Quaternion.Euler(0f, moveAngle, 0f);
        float t = _ncc.Grounded ? GroundTurnLerp : AirTurnLerp;

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, t * Runner.DeltaTime);
    }

    // ★ 공격 입력 처리
    void Combo()
    {
        if (_ncc.Grounded)
        {
            _anim.SetBool("Combo", true);
        }
    }

    public void StartKnockback(Vector3 initialVel)
    {
        CurrentKnockbackVelocity = initialVel;
        KnockbackTimer = 0.7f; // 넉백 지속 시간
    }

    // ★ 매 프레임(Tick)마다 넉백 속도를 줄임
    void ProcessKnockback()
    {
        if (KnockbackTimer > 0)
        {
            // 속도 감쇠 (Lerp 사용)
            Vector3 minVel = CurrentKnockbackVelocity * 0.2f;
            CurrentKnockbackVelocity = Vector3.Lerp(CurrentKnockbackVelocity, minVel, 5f * Runner.DeltaTime);
            
            KnockbackTimer -= Runner.DeltaTime;
        }
        else
        {
            CurrentKnockbackVelocity = Vector3.zero;
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
    }

    public void ResetHit() { _anim.SetInteger("Hit", 0); }
    public void SetKnockedHit() { _anim.SetInteger("Hit", 5); }
}