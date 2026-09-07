using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Fusion;
using System.Collections;

public enum ExtraStatType
{
    Life,
    JumpAbility
}

public class Player : NetworkBehaviour
{
    [SerializeField] NetworkCharacterController _ncc;
    [SerializeField] CharacterController _cc;
    [SerializeField] Animator _anim;
    [SerializeField] Collider _col;

    [SerializeField] BuffSystem _buffSystem;
    [SerializeField] ItemSystem _item;

    CharacterInfo _info;
    public Sprite charIcon;
    [Networked] Vector3 _spawnVec { get; set; }
    [Networked] Quaternion _spawnRot { get; set; }

    Ability _ability;
    public bool hasAbility = false;
    SkillInterface _skill;

    public FollowCamera POV;
    public AttackParameters paramlist;

    [SerializeField] int _modelNum;
    [Networked] public int team { get; set; }

    public PlayerStats stats;
    [SerializeField] int life;
    [SerializeField] int jumpAbiliy;

    public List<GameObject> modelList;

    [Networked, OnChangedRender(nameof(OnModelNumChanged))] public int ModelNum { get; set; }
    [Networked] public NetworkBool IsModelAssigned { get; set; }
    [Networked] public NetworkBool HasSelectedCard { get; set; }

    public void OnModelNumChanged()
    {
        for (int i = 0; i < modelList.Count; i++)
        {
            modelList[i].SetActive(i == ModelNum);
        }

        _info = modelList[ModelNum].GetComponent<CharacterInfo>();
        if (_info != null)
        {
            _anim.avatar = _info.avatar;
            charIcon = _info.icon;

            // Update attack areas owner if necessary, although they might already be set
            foreach (AttackArea area in _info.areas)
            {
                area.SetOwner(this);
                if (!attackAreas.Contains(area))
                    attackAreas.Add(area);
            }
        }
    }

    public UnityEvent onHit;
    public UnityEvent<float> onHPChange;

    // 운동량 관련 변수
    [Networked] Vector3 _horVelocity { get; set; }
    [Networked] float _verVelocity { get; set; }
    [Networked] Vector3 _externalVelocity { get; set; }

    // 이동 관련 변수
    [Networked] float _blendSpeedY { get; set; }
    [Networked] float _blendSpeedX { get; set; }

    // 상태 변수
    [Networked] public bool IsDead { get; set; }
    [Networked] bool IsSuperarmour { get; set; }
    bool _isMoveable = true;
    float _jumpStartTimer;
    int _jumpCount;
    bool jumpedThisFrame;

    // 체력 및 스탯 변수
    [Networked] public float CurrentHP { get; set; }
    [Networked] public int CurrentLives { get; set; }
    [Networked] public int TotalScore { get; set; }
    [Networked, Capacity(5)] public NetworkArray<int> RoundEliminations => default;
    [Networked] public Player LastAttacker { get; set; }
    [Networked] float LastAttackedTimer { get; set; }
    [Networked] float AirborneTimer { get; set; }
    [Networked] bool LongAirborne { get; set; }

    List<AttackArea> attackAreas = new List<AttackArea>();

    // 넉백 관련 네트워크 변수
    [Networked] Vector3 CurrentKnockbackVelocity { get; set; }
    [Networked] float KnockbackTimer { get; set; }
    [Networked] Quaternion RotateVelTarget { get; set; }
    [Networked] float RotateVelTimer { get; set; }
    [Networked] float RespawnTimer { get; set; }
    [Networked] float SlipTimer { get; set; }
    Vector3 MinimunKnockbackVelocity;

    // 끌어당기기(Pull) 관련 네트워크 변수
    [Networked] public bool isPulling { get; set; }
    [Networked] float PullingTimer { get; set; }
    [Networked] Player Puller { get; set; }
    [Networked] float PullTimer { get; set; }
    [Networked] Vector3 PullVelocity { get; set; }

    // 스킬 쿨타임 관련 네트워크 변수
    [Networked] float CurrentCoolDown { get; set; }
    [Networked] float SkillDurationTimer { get; set; } // 스킬 지속시간 체크용

    [Networked, OnChangedRender(nameof(OnNickNameChanged))] public NetworkString<_32> NickName { get; set; }

    public OpponentData linkedOpponentData;

    void OnNickNameChanged()
    {
        if (Object.HasInputAuthority && HUDManager.Instance != null && HUDManager.Instance.charName != null)
        {
            HUDManager.Instance.charName.text = NickName.ToString();
        }

        if (linkedOpponentData != null)
        {
            linkedOpponentData.SetOpponentId(NickName.ToString());
        }
    }

    [SerializeField] private LayerMask _groundLayer;

    [Networked] public int ComboStep { get; set; }
    [Networked] public int HitState { get; set; }

    const float Gravity = -9.81f;
    const float GroundTurnLerp = 5f;
    const float AirTurnLerp = 1.5f;

    public bool comboRegister;

    NetworkButtons _curButtons;
    NetworkButtons _prevButtons;

    public GameObject barrier;

    [SerializeField] ParticleSystem spawnEffect;
    [SerializeField] ParticleSystem hitEffect;
    [SerializeField] AudioSource hitSound;
    [SerializeField] ParticleSystem healEffect;
    [SerializeField] ParticleSystem blinkEffect;
    [SerializeField] ParticleSystem magnetEffect;
    [SerializeField] Blast shockBlast;

    void Awake()
    {
        stats.InitalizeStats();

        shockBlast.SetOwner(this);
    }

    public override void Spawned()
    {
        spawnEffect.Play();

        if (Object.HasStateAuthority)
        {
            if (!IsModelAssigned)
            {
                ModelNum = Random.Range(0, modelList.Count);
                IsModelAssigned = true;
            }
            HasSelectedCard = false;
        }

        // Ensure visuals are updated immediately upon spawning/syncing
        OnModelNumChanged();

        if (Object.HasInputAuthority)
        {
            string myName = DataManager.Instance.UserNickName;
            if (string.IsNullOrEmpty(myName))
            {
                myName = DataManager.Instance.LoadNickName();
            }
            RPC_SetNickName(myName);
        }

        StartCoroutine(WaitForSceneLoad());
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetNickName(string name)
    {
        NickName = name;
    }

    IEnumerator WaitForSceneLoad()
    {
        while (GameManager.Instance == null || FollowCamera.Instance == null || HUDManager.Instance == null)
            yield return null;

        SetupPlayer();
    }

    void SetupPlayer()
    {
        GameManager.Instance.RegisterPlayer(this);

        if (Object.HasStateAuthority)
            life = GameManager.Instance.InitialLives;

        if (Object.HasInputAuthority)
        {
            POV = FollowCamera.Instance;
            POV.target = this;

            HUDManager.Instance.charName.text = NickName.ToString();
            HUDManager.Instance.charPortrait.sprite = charIcon;
            onHPChange.AddListener(HUDManager.Instance.hpBar.UpdateFillBar);
            if (HUDManager.Instance.hpBar != null)
            {
                HUDManager.Instance.hpBar.SetFillBarInstant(1.0f);
            }
            _item.HUDLink();
            _skill = HUDManager.Instance.skillInterface;

            GameManager.Instance.WaitForRegister(this);
        }

        Rpc_BroadcastRoundStart();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_BroadcastSetSpawn(Vector3 pos, Quaternion rot)
    {
        _spawnVec = pos;
        _spawnRot = rot;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_BroadcastRoundStart()
    {
        _cc.enabled = true;
        _ncc.enabled = true;
        _col.enabled = true;

        CurrentLives = life;
        ChangeHP(stats.GetStat(StatType.MaxHP).Value);

        _jumpCount = jumpAbiliy;
        IsDead = false;

        _horVelocity = Vector3.zero;
        _verVelocity = 0f;
        _externalVelocity = Vector3.zero;
        KnockbackTimer = 0f;
        PullTimer = 0f;

        if (_ncc != null)
        {
            _ncc.enabled = true;
            _ncc.Teleport(_spawnVec, _spawnRot);
            Debug.Log("Visible");
            Invoke("PlayerVisible", 0.1f);
        }

        if (_item != null)
            _item.ResetItems();

        if (_ability != null)
            CurrentCoolDown = _ability.coolTime / 2;

        Debug.Log($"[RoundStart] {NickName} initialized with {CurrentLives} lives (from GameManager: {GameManager.Instance.InitialLives}), {CurrentHP} HP, and items reset.");
    }

    public override void Render()
    {
        _anim.SetFloat("MoveSpeedRate", stats.GetStatRate(StatType.SpeedMove));
        _anim.SetFloat("AtkSpeedRate", stats.GetStatRate(StatType.AtkSpeed));

        _anim.SetFloat("SpeedX", _blendSpeedX);
        _anim.SetFloat("SpeedY", _blendSpeedY);
        _anim.SetBool("isDead", IsDead);
        _anim.SetBool("Jump", jumpedThisFrame);
        _anim.SetBool("Airborne", !_ncc.Grounded);
        _anim.SetBool("Long", LongAirborne);
        _anim.SetInteger("ComboStep", ComboStep);
        _anim.SetInteger("Hit", HitState);

        if (ComboStep != 0 && ComboStep != 4 && !Object.HasInputAuthority && !Object.HasStateAuthority)
            _anim.Update(Time.deltaTime);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SelectCard(int rank, int buffIndex)
    {
        HasSelectedCard = true;
        Buff selectedBuff = null;

        switch (rank) {
            case 1: selectedBuff = BuffDB.Instance.GetRank1Buff(buffIndex); break;
            case 2: selectedBuff = BuffDB.Instance.GetRank2Buff(buffIndex); break;
            case 3: selectedBuff = BuffDB.Instance.GetRank3Buff(buffIndex); break;
        }

        if (selectedBuff != null)
        {
            _buffSystem.Rpc_BroadcastApplyBuff(selectedBuff.rank, selectedBuff.buffNum);
            Debug.Log($"서버: {Object.InputAuthority} 플레이어에게 {selectedBuff.name} 버프 적용 완료");
        }
    }

    public override void FixedUpdateNetwork()
    {
        ApplyGravity();
        ProcessKnockback();
        ProcessRotateByVelocity();
        ProcessCoolDown();

        NetworkInputData data = default;

        if (IsDead)
        {
            if (GetInput(out data))
            {
                _curButtons = data.buttons.GetPressed(_prevButtons);
                if (_curButtons.IsSet(InputButton.Attack))
                {
                    //CycleSpectatorTarget();
                }
                _prevButtons = data.buttons;
            }

            if (CurrentLives > 0)
            {
                RespawnTimer -= Runner.DeltaTime;

                if (Object.HasStateAuthority && RespawnTimer <= 0)
                    Rpc_BroadcastRespawn();
            }

            return;
        }

        if (LastAttackedTimer > 0)
            LastAttackedTimer -= Runner.DeltaTime;
        else
            LastAttacker = null;

        if (!_ncc.Grounded)
            AirborneTimer += Runner.DeltaTime;
        else
            AirborneTimer = 0;

        if (AirborneTimer > 0.5f)
            LongAirborne = true;

        _jumpStartTimer = Mathf.Max(0, _jumpStartTimer - Runner.DeltaTime);

        if (_jumpStartTimer <= 0)
            jumpedThisFrame = false;

        if (SlipTimer > 0)
            SlipTimer -= Runner.DeltaTime;

        if (PullingTimer > 0)
            PullingTimer -= Runner.DeltaTime;
        else
        {
            if (isPulling)
            {
                isPulling = false;
                MagnetUsed();
            }
        }

        ProcessPulling();

        if (GetInput(out data))
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
                float decel = SlipTimer > 0 ? 1.2f : 5f;
                _horVelocity = Vector3.Lerp(_horVelocity, Vector3.zero, decel * Runner.DeltaTime);

                //_horVelocity = Vector3.zero;
                _blendSpeedY = Mathf.Lerp(_blendSpeedY, 0, 5f * Runner.DeltaTime);
                _blendSpeedX = Mathf.Lerp(_blendSpeedX, 0, 5f * Runner.DeltaTime);
            }

            if (_curButtons.IsSet(InputButton.Attack))
                Combo();

            ItemUse(data);

            if (_curButtons.IsSet(InputButton.Skill))
                ActivateSkill();
        }

        ComboMove();
        _ncc.Move(_horVelocity + Vector3.up * _verVelocity + _externalVelocity);

        float exdecel = SlipTimer > 0 ? 15f : 25f;
        _externalVelocity = Vector3.Lerp(_externalVelocity, Vector3.zero, exdecel * Runner.DeltaTime);

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
        {
            if (_ncc.Grounded)
            {
                float decel = SlipTimer > 0 ? 1.2f : 25f;
                _horVelocity = Vector3.Lerp(_horVelocity, default, decel * Runner.DeltaTime);
            }
            else
                _horVelocity = Vector3.Lerp(_horVelocity, default, 1.2f * Runner.DeltaTime);
        }
        else
        {
            if (_ncc.Grounded)
            {
                float accel = SlipTimer > 0 ? 3f : 25f;
                _horVelocity = Vector3.ClampMagnitude(_horVelocity + inputDir * accel * Runner.DeltaTime, stats.GetStat(StatType.SpeedMove).Value);
            }
            else
                _horVelocity = Vector3.ClampMagnitude(_horVelocity + inputDir * 3f * Runner.DeltaTime, stats.GetStat(StatType.SpeedMove).Value * 0.7f);
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
        if (!comboRegister)
        {
            if (_ncc.Grounded)
                ComboStep = Mathf.Clamp(ComboStep + 1, 0, 3);
            else
                ComboStep = 4;

            comboRegister = true;
        }
    }

    void ComboMove()
    {
        if (ComboStep != 0 && ComboStep != 4)
        {
            if (Object.HasStateAuthority || Object.HasInputAuthority)
            {
                if (Runner.IsForward)
                {
                    _anim.Update(Runner.DeltaTime);

                    // 이번 네트워크 틱에 발생한 애니메이션 이동량 가로채기
                    Vector3 deltaPosition = _anim.deltaPosition;
                    Quaternion deltaRotation = _anim.deltaRotation;

                    // 이동량을 속도로 변환하여 NCC에 전달
                    _externalVelocity = deltaPosition / (Runner.DeltaTime / 10f);
                    transform.rotation *= deltaRotation;
                }
            }
        }
    }

    void ItemUse(NetworkInputData data)
    {
        if (!Object.HasInputAuthority) return;

        if (data.buttons.IsSet(InputButton.UseItem1))
            _item.Rpc_RequestUseItem(0, this);

        if (data.buttons.IsSet(InputButton.UseItem2))
            _item.Rpc_RequestUseItem(1, this);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_RequestSetAbility(int skillNum)
    {
        if (_ability != null) return;

        HasSelectedCard = true;

        _ability = AbilityDB.Instance.SetAbility(skillNum);
        hasAbility = true;

        CurrentCoolDown = _ability.coolTime / 2;
        SkillDurationTimer = 0f;

        if (Object.HasInputAuthority)
            _skill.SetSkill(_ability);
    }

    void ActivateSkill()
    {
        if (!Object.HasInputAuthority) return;

        if (_ability != null && CurrentCoolDown <= 0 && SkillDurationTimer <= 0)
            Rpc_RequestSkill();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void Rpc_RequestSkill()
    {
        AbilityDB.Instance.ActivateAbility(_ability.skillNum, this);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_BroadcastSkillActivate()
    {
        SkillDurationTimer = _ability.duration;
        CurrentCoolDown = _ability.coolTime;

        if (Object.HasInputAuthority)
            _skill.OnSkillUse();
    }

    public void ApplyHit(Vector3 hitPos, float damage, Vector3 knockDir, float knockPow, float camShake)
    {
        if (IsDead || IsSuperarmour) return;
        if (GameManager.Instance != null && GameManager.Instance.IsTransitioning) return;

        onHit.Invoke();

        ChangeHP(-damage);

        Vector3 kbDir = knockDir.normalized;
        float knockMultiplier = !_ncc.Grounded ? 1.5f : 1.0f;
        float finalKnockPow = (knockPow * knockMultiplier) / Mathf.Max(0.01f, stats.GetStat(StatType.Weight).Value);

        StartKnockback(kbDir * finalKnockPow);

        RPC_BroadcastHitEffect(hitPos, finalKnockPow, camShake);
    }

    public void ApplyHit(Player attacker, Vector3 hitPos, float damage, Vector3 knockDir, float knockPow, float camShake)
    {
        if (IsDead || IsSuperarmour) return;
        if (GameManager.Instance != null && GameManager.Instance.IsTransitioning) return;

        onHit.Invoke();

        if (attacker != null && attacker != this)
        {
            LastAttacker = attacker;
            LastAttackedTimer = 5f;
        }

        ChangeHP(-damage);

        Vector3 kbDir = knockDir.normalized;
        float knockMultiplier = !_ncc.Grounded ? 1.5f : 1.0f;
        float finalKnockPow = (knockPow * knockMultiplier) / Mathf.Max(0.01f, stats.GetStat(StatType.Weight).Value);

        StartKnockback(kbDir * finalKnockPow);

        RPC_BroadcastHitEffect(hitPos, finalKnockPow, camShake);
    }

    public void ApplyDeath()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsTransitioning) return;

        ChangeHP(-stats.GetStat(StatType.MaxHP).Value);

        Invoke("PlayerInvisible", 0.25f);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_SetSpectatorMode()
    {
        Debug.Log($"[SpectatorMode] Disabling visuals for {NickName}");
        foreach (var model in modelList)
        {
            model.SetActive(false);
        }

        _ncc.enabled = false;

        if (Object.HasInputAuthority)
        {
            // Initial spectator target
            CycleSpectatorTarget();
        }
    }

    void CycleSpectatorTarget()
    {
        if (!Object.HasInputAuthority) return;

        Player nextTarget = GameManager.Instance.GetAlivePlayer(POV.target as Player);

        if (nextTarget != null)
        {
            POV.ToggleSoft();
            POV.target = nextTarget;
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_BroadcastHitEffect(Vector3 hitPos, float finalKnockPow, float camShake)
    {
        SetHitAnim(hitPos, finalKnockPow);

        hitEffect.transform.position = transform.position + Vector3.up * 0.85f + (hitPos - transform.position).normalized * 0.3f;
        hitEffect.Play();

        if (hitSound)
        {
            hitSound.pitch = Random.Range(0.8f, 1.2f);
            hitSound.Play();
        }

        if (Object.HasInputAuthority)
            POV.CameraShake(camShake);
    }

    public void ApplyHeal(float ratio)
    {
        ChangeHP(stats.GetStat(StatType.MaxHP).Value * ratio);

        healEffect.Play();
    }

    void ChangeHP(float amount)
    {
        if (!Object.HasStateAuthority) return;

        CurrentHP = Mathf.Clamp(CurrentHP + amount, 0, stats.GetStat(StatType.MaxHP).Value);
        Rpc_BroadcastHpChange();

        if (CurrentHP <= 0)
            Rpc_BroadcastOnDeath();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_BroadcastHpChange()
    {
        onHPChange.Invoke(CurrentHP / stats.GetStat(StatType.MaxHP).Value);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void Rpc_BroadcastOnDeath()
    {
        IsDead = true;
        _ncc.enabled = false;
        _cc.enabled = false;
        _col.enabled = false;

        CurrentLives--;
        Debug.Log($"{NickName} died. Remaining Lives: {CurrentLives}");

        HandleDeath();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void Rpc_BroadcastRespawn()
    {
        PlayerInvisible();
        _cc.enabled = true;
        _ncc.enabled = true;
        _col.enabled = true;

        ChangeHP(stats.GetStat(StatType.MaxHP).Value);
        IsDead = false;

        _horVelocity = Vector3.zero;
        _verVelocity = 0f;
        _externalVelocity = Vector3.zero;
        KnockbackTimer = 0f;
        PullTimer = 0f;

        if (POV)
        {
            POV.ToggleSoft();
            POV.target = this;
        }

        //float randomX = UnityEngine.Random.Range(-10f, 10f);
        //float randomZ = UnityEngine.Random.Range(-10f, 10f);
        //Vector3 spawnPos = new Vector3(randomX, 0f, randomZ);
        //
        //_ncc.Teleport(spawnTrans.position, spawnTrans.rotation);

        _ncc.Teleport(_spawnVec, _spawnRot);
        Invoke("PlayerVisible", 0.1f);

        spawnEffect.Play();
    }

    void HandleDeath()
    {
        if (!Object.HasStateAuthority) return;

        if (LastAttacker != null)
        {
            LastAttacker.TotalScore += 3; // 3 points for elimination

            int curRound = GameManager.Instance.CurrentRound;
            if (curRound >= 1 && curRound <= GameManager.Instance.MaxRounds)
            {
                int currentKills = LastAttacker.RoundEliminations.Get(curRound - 1);
                LastAttacker.RoundEliminations.Set(curRound - 1, currentKills + 1);
            }

            Debug.Log($"{LastAttacker.NickName} eliminated {NickName}! Round eliminations: {LastAttacker.RoundEliminations.Get(curRound - 1)}");
            LastAttacker = null;
        }

        if (CurrentLives <= 0)
        {
            Debug.Log($"{NickName} is out of lives. Entering spectator mode.");
            //RPC_SetSpectatorMode();
        }
        else
            RespawnTimer = 5f;
    }

    void PlayerVisible()
    {
        Debug.Log("Visible2");
        _info.mesh.enabled = true;
    }

    void PlayerInvisible()
    {
        _info.mesh.enabled = false;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_MagnetActivate(float time)
    {
        magnetEffect.Play();

        isPulling = true;
        PullingTimer = time;
    }

    void MagnetUsed()
    {
        magnetEffect.Stop();
    }

    public void ActivateShock()
    {
        shockBlast.SetBlastStrength(stats.GetStat(StatType.PowDam).Value, stats.GetStat(StatType.PowKnock).Value);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_BroadcastBlink()
    {
        if (POV)
            POV.ToggleSoft();

        _ncc.Teleport(transform.position + transform.forward.normalized * 5f);

        blinkEffect.Play();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_BroadcastActivateBarrier()
    {
        barrier.SetActive(true);
    }

    // 쿨타임 및 스킬 지속시간 처리 함수 (코루틴 대체)
    void ProcessCoolDown()
    {
        if (SkillDurationTimer > 0)
            SkillDurationTimer -= Runner.DeltaTime;
        else if (CurrentCoolDown > 0)
        {
            CurrentCoolDown = Mathf.Clamp(CurrentCoolDown - Runner.DeltaTime, 0, _ability.coolTime);

            // UI 업데이트는 로컬 플레이어 화면에서만
            if (Object.HasInputAuthority && _skill != null)
            {
                _skill.CoolRate(_ability.coolTime - CurrentCoolDown);

                if (CurrentCoolDown <= 0)
                    _skill.OnCoolComplete();
            }
        }
    }

    public void SetSlippery(float duration)
    {
        if (SlipTimer < duration)
            SlipTimer = duration;
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
            MinimunKnockbackVelocity = Vector3.zero;
            KnockbackTimer = 0;
        }
    }

    void StartRotateVel(Vector3 tarVel)
    {
        RotateVelTarget = Quaternion.LookRotation(tarVel);
        RotateVelTimer = 0.5f;
    }

    void ProcessRotateByVelocity()
    {
        if (RotateVelTimer > 0f)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, RotateVelTarget, 5f * Runner.DeltaTime);
            RotateVelTimer -= Runner.DeltaTime;
        }
        else
            RotateVelTarget = transform.rotation;
    }

    public void PulledToPoint(Player puller, float duration = 5f)
    {
        Puller = puller;
        PullTimer = duration;
        PullVelocity = Vector3.zero;
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
                PullVelocity += pullVel * Runner.DeltaTime;
                PullVelocity = Vector3.ClampMagnitude(PullVelocity, 15f);
            }
                
            PullVelocity = Vector3.Lerp(PullVelocity, Vector3.zero, 3f * Runner.DeltaTime);

            _externalVelocity += PullVelocity;
        }
        else
        {
            PullVelocity = Vector3.zero;
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

    // Animation Events
    public void EnableMovement() { _isMoveable = true; }
    public void DisableMovement() { _isMoveable = false; }

    public void LandingFin() { LongAirborne  = false; }
    
    public void InAttack(int num) 
    { 
        if (num >= 0 && num < attackAreas.Count)
            attackAreas[num].AttackStart();
    }
    
    public void OutAttack(int num) 
    { 
        if (num >= 0 && num < attackAreas.Count)
            attackAreas[num].AttackEnd(); 
    }

    public void OutAttackAll()
    {
        foreach (AttackArea area in attackAreas)
            area.AttackEnd();
    }
    
    public void SetAttackStats(int num)
    {
        float dam = stats.GetStat(StatType.PowDam).Value;
        float knock = stats.GetStat(StatType.PowKnock).Value;

        foreach (AttackArea area in attackAreas)
            area.SetAttackStatus(paramlist.parameters[num], dam, knock);
    }

    void SetHitAnim(Vector3 hitPoint, float knockDis)
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

    public void ResetCombo() { ComboStep = 0; }

    public void ResetHit() { HitState = 0; }
    public void SetKnockedHit() { HitState = 5; }

    public void SetInvulnerable() { IsSuperarmour = true; }
    public void SetVulnerable() { IsSuperarmour = false; }
}