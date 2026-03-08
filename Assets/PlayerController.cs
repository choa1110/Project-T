using UnityEngine;
using Fusion;
using TMPro;

public class PlayerController : NetworkBehaviour
{
    private NetworkCharacterController control; 
    
    [Header("이동 속도")]
    public float moveSpeed = 5f;

    [Header("닉네임 UI 연결")]
    public TextMeshProUGUI nameText; 

    [Networked, OnChangedRender(nameof(OnNickNameChanged))]
    public NetworkString<_32> NickName { get; set; }

    public override void Spawned()
    {
        control = GetComponent<NetworkCharacterController>();

        if (HasStateAuthority)
        {
            string myName = DataManager.Instance.UserNickName;

            if (string.IsNullOrEmpty(myName))
            {
                myName = DataManager.Instance.LoadNickName();
            }
            
            NickName = myName; 
        }
        else
        {
            nameText.text = NickName.ToString();
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            Vector3 moveDir = new Vector3(data.direction.x, 0, data.direction.y);
            
            control.Move(moveDir * moveSpeed * Runner.DeltaTime); 
        }
    }

    void OnNickNameChanged()
    {
        nameText.text = NickName.ToString();
    }
}