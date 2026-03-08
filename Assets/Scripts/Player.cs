using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Fusion;

public enum ExtraStatType
{
    Life,
    JumpAbility
}

public class Player : NetworkBehaviour
{
    NetworkCharacterController _ncc;
    Animator _anim;

    InputSystem _input;
    ItemSystem _item;

    CharacterInfo _info;

    Ability _ability;
    public SkillInterface skill;

    public FollowCamera POV;
    public AttackParameters paramlist;

    [SerializeField] int _modelNum;
    public int team;
    public PlayerStats stats;
    [SerializeField] int life;
    [SerializeField] int jumpAbiliy;

    public List<GameObject> modelList;

    public UnityEvent onHit;
    public UnityEvent<float> onDamage;

    Vector3 _horVelocity;
    float _verVelocity;
    Vector3 _externalVelocity;

    // 이동 관련 변수
    [Networked] float _blendSpeedY { get; set; }
    [Networked] float _blendSpeedX { get; set; }

    // 상태 변수
    [Networked] public bool IsDead { get; set; }
    bool _isGrounded = true;
    bool _isMoveable = true;
    float _jumpStartTimer;
    int _jumpCount;

    // 체력 및 스탯 변수
    [Networked] public float CurrentHP { get; set; }
    int _curLife;

    List<AttackArea> attackAreas = new List<AttackArea>();

    // 넉백 관련 네트워크 변수
    [Networked] public Vector3 CurrentKnockbackVelocity { get; set; }
    [Networked] public float KnockbackTimer { get; set; }
    
    // 끌어당기기(Pull) 관련 네트워크 변수
    [Networked] public Player PullTarget { get; set; }
    [Networked] public float PullTimer { get; set; }
    [Networked] public Vector3 MagnetVelocity { get; set; }

    // 스킬 쿨타임 관련 네트워크 변수
    [Networked] public float CurrentCoolDown { get; set; }
    [Networked] public float SkillDurationTimer { get; set; } // 스킬 지속시간 체크용

    [SerializeField] private LayerMask _groundLayer;

    const float Gravity = -9.81f;
    const float GroundTurnLerp = 5f;
    const float AirTurnLerp = 1.5f;

    BuffSystem _buffSystem;

    void Awake()
    {
        _ncc = GetComponent<NetworkCharacterController>();
        _anim = GetComponent<Animator>();

        _input = GetComponent<InputSystem>();
        _item = GetComponent<ItemSystem>();

        modelList[_modelNum].SetActive(true);
        _info = modelList[_modelNum].GetComponent<CharacterInfo>();
        _anim.avatar = _info.avatar;

        _buffSystem = GetComponent<BuffSystem>();

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

    public override void Render()
    {
        _anim.SetFloat("MoveSpeedRate", stats.GetStatRate(StatType.SpeedMove));
        _anim.SetFloat("AtkSpeedRate", stats.GetStatRate(StatType.AtkSpeed));
        
        _anim.SetFloat("SpeedX", _blendSpeedX);
        _anim.SetFloat("SpeedY", _blendSpeedY);
        _anim.SetBool("Airborne", !_ncc.Grounded);
    }

    void Update() { }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void Rpc_RequestHitToServer(NetworkObject targetObject, Vector3 hitPos, float damage, Vector3 knockDir, float knockPow, float camShake)
    {
        if(targetObject == null) return;

        Player targetPlayer = targetObject.GetComponent<Player>();
        if(targetPlayer != null && !targetPlayer.IsDead)
        {
            targetPlayer.CurrentHP = Mathf.Clamp(targetPlayer.CurrentHP - damage, 0, targetPlayer.stats.GetStat(StatType.MaxHP).Value);

            float weight = targetPlayer.stats.GetStat(StatType.Weight).Value;
            float knockMultiplier = !targetPlayer._ncc.Grounded ? 1.5f : 1.0f;
            float finalKnockPow = (knockPow * knockMultiplier) / Mathf.Max(0.1f, weight);
            
            Vector3 initialVel = knockDir.normalized * finalKnockPow;

            targetPlayer.StartKnockback(initialVel);
            targetPlayer.RPC_BroadcastHitEffect(hitPos, finalKnockPow, camShake);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_BroadcastHitEffect(Vector3 hitPos, float knockDis, float camShake)
    {
        onHit.Invoke();

        SetHit(hitPos, knockDis);
        if(Object.HasInputAuthority && POV != null)
            POV.CameraShake(camShake);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SelectCard(int buffIndex)
    {
        Buff selectedBuff = null;

        switch(GameManager.instance.CurrentRound){
            case 1: selectedBuff = BuffDB.Instance.GetRank1Buff(buffIndex); break;
            case 2: selectedBuff = BuffDB.Instance.GetRank2Buff(buffIndex); break;
            case 3: selectedBuff = BuffDB.Instance.GetRank3Buff(buffIndex); break;
        }

        if(selectedBuff != null)
        {
            _buffSystem.ApplyBuff(selectedBuff);
            Debug.Log($"서버: {Object.InputAuthority} 플레이어에게 {selectedBuff.name} 버프 적용 완료");
        }
    }
    public override void FixedUpdateNetwork()
    {
        if (IsDead) return;

        if (Object.HasStateAuthority || Object.HasInputAuthority)
        {
            Debug.Log("Processing 1");
            _jumpStartTimer = Mathf.Max(0, _jumpStartTimer - Runner.DeltaTime);
            
            ProcessKnockback();
            ProcessPulling();
            ProcessCoolDown();

            Vector3 moveDirection = Vector3.zero;
            bool isKnockedBack = KnockbackTimer > 0;

            if (GetInput(out NetworkInputData data))
            {
                Debug.Log("Processing 2 " + data);
                if (_isMoveable && !isKnockedBack)
                {
                    
                    Debug.Log("Processing 3");
                    Vector3 inputDir;
                    if (POV != null)
                        inputDir = POV.transform.right * data.direction.x + POV.transform.forward * data.direction.y;
                    else
                        inputDir = new Vector3(data.direction.x, 0, data.direction.y);

                    inputDir.y = 0f;
                    moveDirection = inputDir.normalized;

                    _blendSpeedY = Mathf.Lerp(_blendSpeedY, Sign(data.direction.y), 5f * Runner.DeltaTime);
                    _blendSpeedX = Mathf.Lerp(_blendSpeedX, Sign(data.direction.x), 5f * Runner.DeltaTime);

                    Rotation(data);
                    
                    if (data.buttons.IsSet(InputButtons.Attack)) 
                    {
                        Combo();
                    }

                    if (data.buttons.IsSet(InputButtons.Jump))
                    {
                        if (_jumpStartTimer <= 0)
                        {
                            if (_ncc.Grounded || _jumpCount > 0)
                            {
                                _ncc.Jump();
                                
                                _jumpCount--;
                                _jumpStartTimer = 0.25f;
                            }
                        }
                    }
                }
                else
                {
                    _blendSpeedY = Mathf.Lerp(_blendSpeedY, 0, 5f * Runner.DeltaTime);
                    _blendSpeedX = Mathf.Lerp(_blendSpeedX, 0, 5f * Runner.DeltaTime);
                }
            }

            float currentSpeed = stats.GetStat(StatType.SpeedMove).Value;
            if (!_ncc.Grounded) currentSpeed *= 0.7f;
            
            _ncc.maxSpeed = currentSpeed;
            _ncc.gravity = Gravity;
            _ncc.acceleration = 100f;

            if (isKnockedBack)
            {
                _ncc.Move(CurrentKnockbackVelocity);
            }
            else
            {
                _ncc.Move(moveDirection);
            }

            if (Object.HasInputAuthority)
            {
                ItemUse();
                ActivateSkill();
            }
        }

        if (_ncc.Grounded)
        {
            _jumpCount = jumpAbiliy;
        }
        // _ncc.Move((_horVelocity + Vector3.up * _verVelocity + _externalVelocity) * Runner.DeltaTime);
        // _externalVelocity = Vector3.zero;
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

    void ItemUse()
    {   
        if(Object.HasInputAuthority){
            if (_input.useItem1)
                RPC_UseItemBuff(0);

            if (_input.useItem2)
                RPC_UseItemBuff(1);
        }
    }
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_UseItemBuff(int itemIndex)
    {
        Buff itemBuff = BuffDB.Instance.GetItemBuff(itemIndex);

        if(itemBuff != null)
        {
            GetComponent<BuffSystem>().ApplyBuff(itemBuff);
            Debug.Log($"[서버] {Object.InputAuthority} 플레이어가 {itemIndex}번 아이템 버프를 사용했습니다!");        
        }
    }
    void ActivateSkill()
    {
        if (_ability != null)
        {
            if (_input.skill && CurrentCoolDown <= 0 && SkillDurationTimer <= 0)
            {
                AbilityDB.Instance.ActivateAbility(_ability.skillNum, this);

                SkillDurationTimer = _ability.duration;
                CurrentCoolDown = _ability.coolTime;
                skill.OnSkillUse();
            }
        }
    }

    // 쿨타임 및 스킬 지속시간 처리 함수 (코루틴 대체)
    void ProcessCoolDown()
    {
        if (SkillDurationTimer > 0)
        {
            SkillDurationTimer -= Runner.DeltaTime;
        }
        else if (CurrentCoolDown > 0)
        {
            CurrentCoolDown = Mathf.Clamp(CurrentCoolDown - Runner.DeltaTime, 0, _ability.coolTime);
            
            // UI 업데이트는 로컬 플레이어 화면에서만
            if (Object.HasInputAuthority && skill != null)
            {
                skill.CoolRate(_ability.coolTime - CurrentCoolDown);
                if (CurrentCoolDown <= 0)
                    skill.OnCoolComplete();
            }
        }
    }

    public void ApplyHit(Vector3 pos, float damage, Vector3 knockDir, float knockPow, float camShake)
    {
        if (IsDead) return;

        CurrentHP = Mathf.Clamp(CurrentHP - damage, 0, stats.GetStat(StatType.MaxHP).Value);
        onHit.Invoke();
        onDamage.Invoke(CurrentHP / stats.GetStat(StatType.MaxHP).Value);

        if (!_isGrounded)
            knockPow *= 1.5f;

        Vector3 kbDir = knockDir.normalized;
        float knockDis = knockPow / Mathf.Max(0.1f, stats.GetStat(StatType.Weight).Value);
        Vector3 initialVel = kbDir * knockDis;

        StartKnockback(initialVel);
        SetHit(pos, knockDis);

        if(POV != null) POV.CameraShake(camShake);
    }

    public void ApplyHeal(float amount)
    {
        CurrentHP = Mathf.Clamp(CurrentHP + amount, 0, stats.GetStat(StatType.MaxHP).Value);
        onDamage.Invoke(CurrentHP / stats.GetStat(StatType.MaxHP).Value);
    }

    public void SetAbility(Ability ability)
    {
        _ability = ability;
        CurrentCoolDown = _ability.coolTime / 2;
        SkillDurationTimer = 0f;

        skill.SetSkill(_ability);
    }

    public void StartKnockback(Vector3 initialVel)
    {
        CurrentKnockbackVelocity = initialVel;
        KnockbackTimer = 0.5f; 
    }

    void ProcessKnockback()
    {
        if (KnockbackTimer > 0)
        {
            CurrentKnockbackVelocity = Vector3.Lerp(CurrentKnockbackVelocity, Vector3.zero, 5f * Runner.DeltaTime);
            KnockbackTimer -= Runner.DeltaTime;

            if (KnockbackTimer <= 0)
            {
                CurrentKnockbackVelocity = Vector3.zero;
                KnockbackTimer = 0;
            }
        }
    }

    public void PulledToPoint(Player puller, float duration = 5f)
    {
        PullTarget = puller;
        PullTimer = duration;
        MagnetVelocity = Vector3.zero;
    }

    void ProcessPulling()
    {
        if (PullTimer > 0 && PullTarget != null)
        {
            PullTimer -= Runner.DeltaTime;

            Vector3 targetPos = PullTarget.transform.position;
            Vector3 dir = targetPos - transform.position;
            float distance = dir.magnitude;

            if (distance > 0.05f)
            {
                dir.Normalize();
                float distanceFactor = Mathf.Clamp01(distance / 2f);

                Vector3 pullVel = dir * 30f * distanceFactor;
                MagnetVelocity += pullVel * Runner.DeltaTime;
                MagnetVelocity = Vector3.ClampMagnitude(MagnetVelocity, 15f);
            }

            if (PullTimer <= 0)
            {
                MagnetVelocity = Vector3.zero;
                PullTarget = null;
            }
        }
        else
        {
            MagnetVelocity = Vector3.Lerp(MagnetVelocity, Vector3.zero, 3f * Runner.DeltaTime);
        }

        _externalVelocity += MagnetVelocity;
    }

    static float Sign(float v)
    {
        if (v < 0.01f && v > -0.01f) return 0f;
        return v > 0 ? 1f : -1f;
    }

    public void ExtraStatModify(ExtraStatType targetStat, int amount)
    {
        switch (targetStat)
        {
            case ExtraStatType.Life:
                life += amount;
                break;
            case ExtraStatType.JumpAbility:
                jumpAbiliy += amount;
                break;
        }
    }

    public Player GetClosestOpponent()
    {
        return this;
    }

    // Animation Events
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
        Vector3 velocity = _ncc.Velocity; 
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
    }

    public void ResetHit() { _anim.SetInteger("Hit", 0); }
    public void SetKnockedHit() { _anim.SetInteger("Hit", 5); }
}