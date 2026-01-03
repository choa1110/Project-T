using UnityEngine;
using Fusion;

public class PlayerController : NetworkBehaviour
{
    private NetworkCharacterController control;

    public override void Spawned()
    {
        control = GetComponent<NetworkCharacterController>();
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            Vector3 moveDir = new Vector3(data.direction.x, 0, data.direction.y);
            control.Move(moveDir);
        }
    }
}