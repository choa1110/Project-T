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

    BuffSystem _buffSystem;
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

    [Networked] Vector3 _horVelocity { get; set; }
    [Networked] float _verVelocity { get; set; }
    [Networked] Vector3 _externalVelocity { get; set; }

    // 이동 관련 변수
    [Networked] float _blendSpeedY { get; set; }
    [Networked] float _blendSpeedX { get; set; }

    // 상태 변수
    [Networked] public bool IsDead { get; set; }
    bool _isMoveable = true;
    float _jumpStartTimer;
    int _jumpCount;
    bool jumpedThisFrame;

    // 체력 및 스탯 변수
    [Networked] public float CurrentHP { get; set; }
    int _curLife;

    List<AttackArea> attackAreas = new List<AttackArea>();

    // 넉백 관련 네트워크 변수
    [Networked] Vector3 CurrentKnockbackVelocity { get; set; }
    [Networked] float KnockbackTimer { get; set; }
    [Networked] Quaternion RotateVelTarget { get; set; }
    [Networked] float RotateVelTimer { get; set; }
    Vector3 MinimunKnockbackVelocity;

    // 끌어당기기(Pull) 관련 네트워크 변수
    [Networked] public Player Puller { get; set; }
    [Networked] public float PullTimer { get; set; }
    [Networked] public Vector3 MagnetVelocity { get; set; }

    // 스킬 쿨타임 관련 네트워크 변수
    [Networked] public float CurrentCoolDown { get; set; }
    [Networked] public float SkillDurationTimer { get; set; } // 스킬 지속시간 체크용

    [SerializeField] private LayerMask _groundLayer;

    [Networked] public int ComboStep { get; set; }
    [Networked] public int HitState { get; set; }

    const float Gravity = -9.81f;
    const float GroundTurnLerp = 5f;
    const float AirTurnLerp = 1.5f;

    public bool comboRegister;

    NetworkButtons _curButtons;
    NetworkButtons _prevButtons;

    void Awake()
    {
        _ncc = GetComponent<NetworkCharacterController>();
        _anim = GetComponent<Animator>();

        _buffSystem = GetComponent<BuffSystem>();
        _item = GetComponent<ItemSystem>();

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
        if (Object.HasInputAuthority)
        {
            POV = FindFirstObjectByType<FollowCamera>();
            Debug.Log(POV);
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
        _anim.SetBool("Jump", jumpedThisFrame);
        _anim.SetBool("Airborne", !_ncc.Grounded);
        _anim.SetInteger("ComboStep", ComboStep);
        _anim.SetInteger("Hit", HitState);
    }

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
            targetPlayer.SetHit(hitPos, finalKnockPow);

            if (POV != null)
                POV.CameraShake(camShake);

            targetPlayer.RPC_BroadcastHitEffect();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_BroadcastHitEffect()
    {
        onHit.Invoke();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SelectCard(int buffIndex)
    {
        Buff selectedBuff = null;

        switch(GameManager.Instance.CurrentRound){
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
        ApplyGravity();

        if (IsDead) return;

        _jumpStartTimer = Mathf.Max(0, _jumpStartTimer - Runner.DeltaTime);

        if (_jumpStartTimer <= 0)
            jumpedThisFrame = false;

        ProcessKnockback();
        ProcessRotateByVelocity();
        ProcessPulling();
        ProcessCoolDown();

        if (GetInput(out NetworkInputData data))
        {
            _curButtons = data.buttons.GetPressed(_prevButtons);

            if (_isMoveable)
            {
                Movement(data.move);
                Rotation(data.move);

                if (_curButtons.IsSet(InputButton.Jump))
                    Jump();
            }
            else
            {
                _horVelocity = Vector3.zero;
                _blendSpeedY = Mathf.Lerp(_blendSpeedY, 0, 5f * Runner.DeltaTime);
                _blendSpeedX = Mathf.Lerp(_blendSpeedX, 0, 5f * Runner.DeltaTime);
            }
        }

        if (_curButtons.IsSet(InputButton.Attack))
            Combo();

        ItemUse(data);

        if (_curButtons.IsSet(InputButton.Skill))
            ActivateSkill();

        _ncc.Move(_horVelocity + Vector3.up * _verVelocity + _externalVelocity);
        _externalVelocity = Vector3.zero;

        _prevButtons = data.buttons;
    }

    void ApplyGravity()
    {
        if (!_ncc.Grounded)
            _verVelocity += Gravity * Runner.DeltaTime;
        else
        {
            _verVelocity = -2f;
            _jumpCount = jumpAbiliy;
        }
    }

    void Movement(Vector2 input)
    {
        Vector3 inputDir = new Vector3(input.x, 0f, input.y);
        inputDir = inputDir.normalized;

        _blendSpeedY = Mathf.Lerp(_blendSpeedY, Sign(input.y), 5f * Runner.DeltaTime);
        _blendSpeedX = Mathf.Lerp(_blendSpeedX, Sign(input.x), 5f * Runner.DeltaTime);

        if (inputDir == default)
            _horVelocity = Vector3.Lerp(_horVelocity, default, 25f * Runner.DeltaTime);
        else
        {
            if (_ncc.Grounded)
                _horVelocity = Vector3.ClampMagnitude(_horVelocity + inputDir * 20f * Runner.DeltaTime, stats.GetStat(StatType.SpeedMove).Value);
            else
                _horVelocity = Vector3.ClampMagnitude(_horVelocity + inputDir * 10f * Runner.DeltaTime, stats.GetStat(StatType.SpeedMove).Value * 0.7f);
        }
    }

    void Jump()
    {
        if (_jumpStartTimer <= 0 && _jumpCount > 0)
        {
            _jumpStartTimer = 0.2f;
            _jumpCount--;
            _verVelocity = Mathf.Sqrt(2f * -Gravity * stats.GetStat(StatType.JumpHeight).Value);

            jumpedThisFrame = true;
        }
    }

    void Rotation(Vector2 input)
    {
        if (input == Vector2.zero)
            return;

        float moveAngle = Mathf.Atan2(input.x, input.y) * Mathf.Rad2Deg;

        Quaternion targetRot = Quaternion.Euler(0f, moveAngle, 0f);
        float t = _ncc.Grounded ? GroundTurnLerp : AirTurnLerp;

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, t * Runner.DeltaTime);
    }

    void Combo()
    {
        if (_ncc.Grounded && !comboRegister)
        {
            ComboStep = Mathf.Clamp(ComboStep + 1, 0, 3);

            comboRegister = true;
        }
    }

    void ItemUse(NetworkInputData data)
    {
        if (!Object.HasStateAuthority)
            return;

        if (data.buttons.IsSet(InputButton.UseItem1))
            _item.UseItem(this, 0);

        if (data.buttons.IsSet(InputButton.UseItem2))
            _item.UseItem(this, 1);
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
        if (!Object.HasStateAuthority)
            return;

        if (_ability != null)
        {
            if (CurrentCoolDown <= 0 && SkillDurationTimer <= 0)
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

    //public void ApplyHit(Vector3 pos, float damage, Vector3 knockDir, float knockPow, float camShake)
    //{
    //    if (IsDead) return;
    //
    //    CurrentHP = Mathf.Clamp(CurrentHP - damage, 0, stats.GetStat(StatType.MaxHP).Value);
    //    onHit.Invoke();
    //    onDamage.Invoke(CurrentHP / stats.GetStat(StatType.MaxHP).Value);
    //
    //    if (!_ncc.Grounded)
    //        knockPow *= 1.5f;
    //
    //    Vector3 kbDir = knockDir.normalized;
    //    float knockDis = knockPow / Mathf.Max(0.1f, stats.GetStat(StatType.Weight).Value);
    //    Vector3 initialVel = kbDir * knockDis;
    //
    //    StartKnockback(initialVel);
    //    SetHit(pos, knockDis);
    //
    //    if (POV != null)
    //        POV.CameraShake(camShake);
    //}

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

    void StartKnockback(Vector3 initialVel)
    {
        CurrentKnockbackVelocity = initialVel;
        MinimunKnockbackVelocity = initialVel * 0.1f;
        KnockbackTimer = 0.7f; 
    }

    void ProcessKnockback()
    {
        if (KnockbackTimer > 0 || !_ncc.Grounded)
        {
            CurrentKnockbackVelocity = Vector3.Lerp(CurrentKnockbackVelocity, MinimunKnockbackVelocity, 5f * Runner.DeltaTime);
            KnockbackTimer -= Runner.DeltaTime;

            _externalVelocity += CurrentKnockbackVelocity;
        }
        else
        {
            CurrentKnockbackVelocity = Vector3.zero;
            KnockbackTimer = 0;
        }
    }

    void StartRotateVel(Vector3 tarVel)
    {
        RotateVelTarget = Quaternion.LookRotation(tarVel);
        RotateVelTimer = 0.3f;
    }

    void ProcessRotateByVelocity()
    {
        if (RotateVelTimer > 0f)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, RotateVelTarget, 3.3f * Runner.DeltaTime);
            RotateVelTimer -= Runner.DeltaTime;
        }
        else
            RotateVelTarget = transform.rotation;
    }

    public void PulledToPoint(Player puller, float duration = 5f)
    {
        Puller = puller;
        PullTimer = duration;
        MagnetVelocity = Vector3.zero;
    }

    void ProcessPulling()
    {
        if (PullTimer > 0 && Puller != null)
        {
            PullTimer -= Runner.DeltaTime;

            Vector3 targetPos = Puller.transform.position;
            targetPos += (transform.position - targetPos).normalized;

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
            else
                MagnetVelocity = Vector3.Lerp(MagnetVelocity, Vector3.zero, 3f * Runner.DeltaTime);

            _externalVelocity += MagnetVelocity;
        }
        else
        {
            MagnetVelocity = Vector3.zero;
            Puller = null;
        }
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

    public void ResetCombo() { ComboStep = 0; }

    void SetHit(Vector3 hitPoint, float knockDis)
    {
        Vector3 velocity = transform.position - hitPoint;
        velocity.y = 0;

        if (Vector3.Angle(transform.forward, hitPoint - transform.position) > 90)
        {
            if (knockDis > 20f)
                HitState = 4;
            else
                HitState = 2;
        }
        else
        {
            if (knockDis > 20f)
                HitState = 3;
            else
                HitState = 1;

            velocity *= -1;
        }

        StartRotateVel(velocity);
    }

    public void ResetHit() { HitState = 0; }
    public void SetKnockedHit() { HitState = 5; }
}