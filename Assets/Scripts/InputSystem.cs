using UnityEngine;
using UnityEngine.InputSystem;

public class InputSystem : MonoBehaviour
{
    PlayerInput _input;

    public Vector2 move { get; private set; }

    void Awake()
    {
        _input = GetComponent<PlayerInput>();
    }

    void OnMove(InputValue value)
    {
        move = value.Get<Vector2>();
    }
}