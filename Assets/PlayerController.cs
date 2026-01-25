// using UnityEngine;
// using Fusion;

// public class PlayerController : NetworkBehaviour
// {
//     private NetworkCharacterController control;

//     public override void Spawned()
//     {
//         control = GetComponent<NetworkCharacterController>();
//     }

//     public override void FixedUpdateNetwork()
//     {
//         if (GetInput(out NetworkInputData data))
//         {
//             Vector3 moveDir = new Vector3(data.direction.x, 0, data.direction.y);
//             control.Move(moveDir);
//         }
//     }
// }

using UnityEngine;
using Fusion;
using TMPro; // 텍스트 UI 사용을 위해 필수

public class PlayerController : NetworkBehaviour
{
    // 질문자님의 코드에 맞춰 NetworkCharacterController 사용
    private NetworkCharacterController control; 
    
    [Header("이동 속도")]
    public float moveSpeed = 5f;

    [Header("닉네임 UI 연결")]
    // 유니티 인스펙터에서 머리 위의 TextMeshProUGUI를 드래그해서 넣으세요
    public TextMeshProUGUI nameText; 

    // ★ 네트워크 변수 (닉네임 동기화용)
    // 값이 변경되면 OnNickNameChanged 함수가 실행되어 화면을 갱신합니다.
    [Networked, OnChangedRender(nameof(OnNickNameChanged))]
    public NetworkString<_32> NickName { get; set; }

    public override void Spawned()
    {
        // 1. 컴포넌트 가져오기
        control = GetComponent<NetworkCharacterController>();

        // 2. 닉네임 설정 (내 캐릭터일 때만 실행)
        if (HasStateAuthority)
        {
            // ★ DataManager에서 저장된 이름 가져오기
            // 보여주신 DataManager 코드의 UserNickName 변수를 가져옵니다.
            string myName = DataManager.Instance.UserNickName;

            // 만약 이름이 비어있다면 LoadNickName()으로 다시 한 번 시도 (안전장치)
            if (string.IsNullOrEmpty(myName))
            {
                myName = DataManager.Instance.LoadNickName();
            }
            
            // 네트워크 변수에 넣기 (이제 모든 사람에게 전파됨)
            NickName = myName; 
        }
        else
        {
            // 다른 사람 캐릭터라면, 이미 네트워크로 받아온 이름을 화면에 표시
            nameText.text = NickName.ToString();
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            Vector3 moveDir = new Vector3(data.direction.x, 0, data.direction.y);
            
            // 이동 로직
            control.Move(moveDir * moveSpeed * Runner.DeltaTime); 
        }
    }

    // 닉네임이 변경될 때마다(처음 접속 시 포함) 호출되는 함수
    void OnNickNameChanged()
    {
        nameText.text = NickName.ToString();
    }
}