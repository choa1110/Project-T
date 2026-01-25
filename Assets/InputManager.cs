using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public Vector2 move;
    public bool attack;
    void OnMove(InputValue value)
    {
        move = value.Get<Vector2>();
    }
    void OnAttack(InputValue value)
    {
        attack = value.isPressed;
    }
}
