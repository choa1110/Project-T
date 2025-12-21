using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public struct HitInfo
{
    public GameObject victim;
    public GameObject attacker;

    public HitInfo(GameObject victim, GameObject attacker)
    {
        this.victim = victim;
        this.attacker = attacker;
    }
}

public class Player : MonoBehaviour
{
    CharacterController _controller;
    InputSystem _input;

    public PlayerStats stats;
    public UnityEvent<HitInfo> onHit;
    Vector3 _velocity;

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _input = GetComponent<InputSystem>();

        stats.InitalizeStats();
    }

    void Update()
    {
        if (Keyboard.current.hKey.wasPressedThisFrame)
        {
            onHit.Invoke(new HitInfo(gameObject, gameObject));
        }

        Vector3 inputDir = transform.right * _input.move.x + transform.forward * _input.move.y;
        inputDir.y = 0f;

        _velocity = stats.GetStat(StatType.SpeedMove).Value * inputDir.normalized;

        _controller.Move(_velocity * Time.deltaTime);
    }
}