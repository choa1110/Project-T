using UnityEngine;
using Fusion;

public class ItemBox : NetworkBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        // 1. 가장 먼저! 물리적 충돌 자체가 일어났는지 확인
        Debug.Log($"<color=yellow>[ItemBox] 누군가 닿았습니다! 부딪힌 오브젝트: {other.gameObject.name}</color>");

        if (Object == null || !Object.IsValid) return;

        // 2. 서버에서만 판정하는게 맞는지 확인
        if (!Object.HasStateAuthority) 
        {
            // 클라이언트 화면에서는 여기서 리턴되는게 맞습니다.
            return; 
        }

        Debug.Log("[ItemBox] 서버(방장) 권한 확인 통과!");

        // 3. 부딪힌 대상에게서 ItemSystem을 제대로 찾았는지 확인
        ItemSystem sys = other.GetComponentInParent<ItemSystem>();

        if (sys != null)
        {
            Debug.Log($"<color=green>[ItemBox] {other.name}에서 ItemSystem을 찾았습니다! 아이템 지급 시도...</color>");
            
            int randomItemId = ItemDB.Instance.GetRandomItemID();
            
            if(sys.GiveItem(randomItemId))
            {
                Debug.Log("<color=cyan>[ItemBox] 아이템 지급 성공! 상자를 파괴합니다.</color>");
                Runner.Despawn(Object);
            }
            else
            {
                Debug.Log("<color=red>[ItemBox] 아이템 지급 실패 (인벤토리가 꽉 찼을 수 있음)</color>");
            }
        }
        else
        {
            Debug.Log($"<color=orange>[ItemBox] 부딪힌 {other.name}에게서 ItemSystem을 찾을 수 없습니다!</color>");
        }
    }
}