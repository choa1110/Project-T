using UnityEngine;
using Fusion; // 퓨전 네임스페이스 필수

// MonoBehaviour -> NetworkBehaviour로 변경
public class PlayerController : NetworkBehaviour
{
    public float moveSpeed = 5;

    // CharacterController는 Fusion의 NetworkCharacterController를 쓰는 게 좋지만,
    // 일단 기존 컴포넌트를 그대로 쓰시려면 아래처럼 처리해야 합니다.
    private CharacterController control;

    void Awake()
    {
        control = GetComponent<CharacterController>();
    }

    // ★ Fusion의 핵심: Update 대신 이걸 씁니다.
    public override void FixedUpdateNetwork()
    {
        // 1. 입력값 가져오기 (내 거든 남의 거든 서버가 처리)
        // GetInput은 클라이언트가 보낸 패킷을 서버가 까보는 함수입니다.
        if (GetInput(out NetworkInputData data))
        {
            // 2. 입력값이 있다면 이동 로직 수행
            // data.direction에 클라이언트가 누른 키 정보가 들어있습니다.
            Vector3 moveDir = new Vector3(data.direction.x, 0, data.direction.y);

            // 3. 이동 실행 (Runner.DeltaTime을 써야 동기화가 정확함)
            control.Move(moveDir * moveSpeed * Runner.DeltaTime);
        }
    }
}