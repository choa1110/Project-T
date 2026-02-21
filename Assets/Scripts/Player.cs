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

    // 이동 관련 변수 - Fusion 2에서는 애니메이션 동기화를 위해 Networked 권장
    [Networked] float _blendSpeedY { get; set; }
    [Networked] float _blendSpeedX { get; set; }

    // 상태 변수
    [Networked] public bool IsDead { get; set; } 
    bool _isMoveable = true;
    float _jumpStartTimer;
    int _jumpCount;

    // 체력은 중요하므로 Networked로 관리 (간단한 구현)
    [Networked] public float CurrentHP { get; set; }
    int _curLife;

    List<AttackArea> attackAreas = new List<AttackArea>();

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
        if (Object.HasStateAuthority)
        {
            CurrentHP = stats.GetStat(StatType.MaxHP).Value;
            _curLife = life;
            _jumpCount = jumpAbiliy;
        }
    }

    // Fusion 2: Visual updates and animation blending should happen in Render for maximum smoothness
    public override void Render()
    {
        _anim.SetFloat("MoveSpeedRate", stats.GetStatRate(StatType.SpeedMove));
        _anim.SetFloat("AtkSpeedRate", stats.GetStatRate(StatType.AtkSpeed));
        
        // 애니메이션 파라미터 적용 (Networked 변수를 사용하여 모든 클라이언트에서 동일하게 보임)
        _anim.SetFloat("SpeedX", _blendSpeedX);
        _anim.SetFloat("SpeedY", _blendSpeedY);
        _anim.SetBool("Airborne", !_ncc.Grounded);
    }

    // Update는 비워두거나 제거해도 됩니다.
    void Update() { }

    // 클라이언트가 호스트에게 때렸다고 요청
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void Rpc_RequestHitToServer(NetworkObject targetObject, Vector3 hitPos, float damage, Vector3 knockDir, float knockPow, float camShake)
    {
        if(targetObject == null) return;

        Player targetPlayer = targetObject.GetComponent<Player>();
        if(targetPlayer != null && !targetPlayer.IsDead)
        {
            targetPlayer.CurrentHP = Mathf.Clamp(targetPlayer.CurrentHP - damage, 0, targetPlayer.stats.GetStat(StatType.MaxHP).Value);

            // 넉백 계산: 공중 피격 시 가중치 및 무게 반영
            float weight = targetPlayer.stats.GetStat(StatType.Weight).Value;
            float knockMultiplier = !targetPlayer._ncc.Grounded ? 1.5f : 1.0f;
            float finalKnockPow = (knockPow * knockMultiplier) / Mathf.Max(0.1f, weight);
            
            Vector3 initialVel = knockDir.normalized * finalKnockPow;

            targetPlayer.StartKnockback(initialVel);
            targetPlayer.RPC_BroadcastHitEffect(hitPos, finalKnockPow, camShake);
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
        if (IsDead) return;

        // Fusion 2: 이동 로직은 Authority(서버) 혹은 InputAuthority(로컬 플레이어)만 실행하도록 제한
        if (Object.HasStateAuthority || Object.HasInputAuthority)
        {
            _jumpStartTimer = Mathf.Max(0, _jumpStartTimer - Runner.DeltaTime);
            ProcessKnockback();

            Vector3 moveDirection = Vector3.zero;
            bool isKnockedBack = KnockbackTimer > 0;

            if (GetInput(out NetworkInputData data))
            {
                if (_isMoveable && !isKnockedBack)
                {
                    // Movement calculation
                    Vector3 inputDir;
                    if (POV != null)
                        inputDir = POV.transform.right * data.direction.x + POV.transform.forward * data.direction.y;
                    else
                        inputDir = new Vector3(data.direction.x, 0, data.direction.y);

                    inputDir.y = 0f;
                    moveDirection = inputDir.normalized;

                    // Networked 변수 업데이트 (Render에서 사용됨)
                    _blendSpeedY = Mathf.Lerp(_blendSpeedY, Sign(data.direction.y), 5f * Runner.DeltaTime);
                    _blendSpeedX = Mathf.Lerp(_blendSpeedX, Sign(data.direction.x), 5f * Runner.DeltaTime);

                    Rotation(data);
                    
                    if (data.buttons.IsSet(InputButtons.Attack)) 
                    {
                        Combo();
                    }
                }
                else
                {
                    _blendSpeedY = Mathf.Lerp(_blendSpeedY, 0, 5f * Runner.DeltaTime);
                    _blendSpeedX = Mathf.Lerp(_blendSpeedX, 0, 5f * Runner.DeltaTime);
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
                _ncc.Move(moveDirection);
            }
        }

        if (_ncc.Grounded)
        {
            _jumpCount = jumpAbiliy;
        }
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
        KnockbackTimer = 0.5f; // 약간 단축하여 반응성 개선
    }

    void ProcessKnockback()
    {
        if (KnockbackTimer > 0)
        {
            // 속도 감쇠 (Damping) - Lerp를 사용하여 부드럽게 감속
            CurrentKnockbackVelocity = Vector3.Lerp(CurrentKnockbackVelocity, Vector3.zero, 5f * Runner.DeltaTime);
            KnockbackTimer -= Runner.DeltaTime;

            if (KnockbackTimer <= 0)
            {
                CurrentKnockbackVelocity = Vector3.zero;
                KnockbackTimer = 0;
            }
        }
    }

    // 유틸리티
    static float Sign(float v)
    {
        if (v < 0.01f && v > -0.01f) return 0f;
        return v > 0 ? 1f : -1f;
    }

    // Animation Events 및 기타 함수들
    public void EnableMovement() { _isMoveable = true; }
    public void DisableMovement() { _isMoveable = false; }
    
    public void InAttack(int num) 
    { 
        if(num >= 0 && num < attackAreas.Count)
            attackAreas[num].AttackStart(); 
    }
    
    public void OutAttack(int num) 
    { 
        if(num >= 0 && num < attackAreas.Count)
            attackAreas[num].AttackEnd(); 
    }
    
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
        if (hitDis > 15f) _anim.SetInteger("Hit", 3);
        else _anim.SetInteger("Hit", 1);
    }

    public void ResetHit() { _anim.SetInteger("Hit", 0); }
    public void SetKnockedHit() { _anim.SetInteger("Hit", 5); }
}