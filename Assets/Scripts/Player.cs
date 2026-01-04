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

    public Transform POV;

    [SerializeField] int _modelNum;
    public int team;
    public PlayerStats stats;
    public int jumpAbiliy = 1;

    public List<GameObject> modelList;

    public UnityEvent onHit;

    Vector3 _horVelocity;
    float _verVelocity;
    float _blendSpeedY;
    float _blendSpeedX;

    bool isDead;
    bool isGrounded = true;
    bool isMoveable = true;
    bool _jumpedThisFrame = false;
    float _jumpStartTimer;

    int jumpCount;

    [SerializeField] private LayerMask groundLayer;

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
            groundLayer
        );

        // 히트 여부에 따라 색상 변경
        Gizmos.color = hit ? Color.green : Color.red;

        // 시작 Sphere
        Gizmos.DrawWireSphere(origin, sphereRadius);

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

        stats.InitalizeStats();
        jumpCount = jumpAbiliy;
    }

    void Update()
    {
        _anim.SetFloat("MoveSpeedRate", stats.GetStatRate(StatType.SpeedMove));
        _anim.SetFloat("AtkSpeedRate", stats.GetStatRate(StatType.AtkSpeed));
        _jumpStartTimer = Mathf.Max(0, _jumpStartTimer - Time.deltaTime);

        GroundCheck();
        ApplyGravity();

        if (!isDead)
        {
            if (isMoveable)
            {
                Movement();
                Rotation();
            }
            else
                _horVelocity = Vector3.zero;

            Combo();
        }

        if (Keyboard.current.hKey.wasPressedThisFrame)
        {
            // 테스트용
            onHit.Invoke();
        }

        _controller.Move((_horVelocity + Vector3.up * _verVelocity) * Time.deltaTime);
    }

    void GroundCheck()
    {
        Ray groundRay = new Ray(transform.position + Vector3.up * 0.3f, Vector3.down);

        if (Physics.SphereCast(groundRay, 0.3f, 0.3f, groundLayer) && _verVelocity <= 0)
        {
            isGrounded = true;
            jumpCount = jumpAbiliy;
        }
        else
            isGrounded = false;

        _anim.SetBool("Airborne", !isGrounded);
        //if (!isGrounded)
        //{
        //    Debug.LogError(isGrounded);
        //}
    }

    void ApplyGravity()
    {
        if (!isGrounded)
            _verVelocity += Gravity * Time.deltaTime;
        else if (_jumpStartTimer <= 0)
            _verVelocity = SticktoGround;
    }

    void Movement()
    {
        Vector3 inputDir = POV.right * _input.move.x + POV.forward * _input.move.y;
        inputDir.y = 0f;

        _blendSpeedY = Mathf.Lerp(_blendSpeedY, Sign(_input.move.y), 5f * Time.deltaTime);
        _blendSpeedX = Mathf.Lerp(_blendSpeedX, Sign(_input.move.x), 5f * Time.deltaTime);
        _anim.SetFloat("SpeedX", _blendSpeedX);
        _anim.SetFloat("SpeedY", _blendSpeedY);

        if (_input.jump && !_jumpedThisFrame && jumpCount > 0)
        {
            _jumpStartTimer = 0.15f;
            jumpCount--;
            _verVelocity = Mathf.Sqrt(2f * -Gravity * stats.GetStat(StatType.JumpHeight).Value);

            _anim.SetTrigger("Jump");
        }

        if (isGrounded)
            _horVelocity = stats.GetStat(StatType.SpeedMove).Value * inputDir.normalized;
        else
            _horVelocity = stats.GetStat(StatType.SpeedMove).Value * 0.7f * inputDir.normalized;

        _jumpedThisFrame = _input.jump;
    }

    void Rotation()
    {
        if (_input.move == Vector2.zero)
            return;

        float moveAngle = Mathf.Atan2(_input.move.x, _input.move.y) * Mathf.Rad2Deg + POV.eulerAngles.y;
        Quaternion targetRot = Quaternion.Euler(0f, moveAngle, 0f);
        float t = isGrounded ? GroundTurnLerp : AirTurnLerp;

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, t * Time.deltaTime);
    }

    void Combo()
    {
        if (_input.attack && isGrounded)
        {
            _anim.SetBool("Combo", true);
        }
    }

    public void ApplyHit(Player causer, float damage, Vector3 knockDir, float knockPow)
    {

    }

    public void ApplyDamage(float damage, bool isflinch = false)
    {

    }

    static float Sign(float v)
    {
        if (v < 0f) return -1f;
        if (v > 0f) return 1f;
        return 0f;
    }

    // Animation Events
    public void EnableMovement() { isMoveable = true; }
    public void DisableMovement() { isMoveable = false; }

    public void ResetCombo()
    {
        _anim.SetBool("Combo", false);
    }
}