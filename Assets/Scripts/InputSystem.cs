using UnityEngine;
using UnityEngine.InputSystem;

public class InputSystem : MonoBehaviour
{
    PlayerInput _input;

    public Vector2 move { get; private set; }
    public bool jump { get; private set; }
    public bool attack { get; private set; }
    public bool sprint { get; private set; }
    public bool guard { get; private set; }
    public bool useItem1 { get; private set; }
    public bool useItem2 { get; private set; }

    void Awake()
    {
        _input = GetComponent<PlayerInput>();
    }

    void OnMove(InputValue value)
    {
        move = value.Get<Vector2>();
    }

    void OnJump(InputValue value)
    {
        jump = value.isPressed;
    }

    void OnAttack(InputValue value)
    {
        attack = value.isPressed;
    }

    void OnSprint(InputValue value)
    {
        sprint = value.isPressed;
    }

    void OnGuard(InputValue value)
    {
        guard = value.isPressed;
    }

    void OnUseItem1(InputValue value)
    {
        useItem1 = value.isPressed;
    }

    void OnUseItem2(InputValue value)
    {
        useItem2 = value.isPressed;
    }
}