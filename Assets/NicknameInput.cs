using UnityEngine;

public class NicknameInput : MonoBehaviour
{
    public void SetNickname(string nickname)
    {
        DataManager.Instance.SetNickName(nickname);
        Destroy(gameObject);
    }
}
