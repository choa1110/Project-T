using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance;

    public string UserNickName { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    
    public void SetNickName(string name)
    {
        UserNickName = name;
        PlayerPrefs.SetString("MyNickName", name); //우선 컴퓨터에 이후 데베
        PlayerPrefs.Save();
    }

    public string LoadNickName()
    {
        if (PlayerPrefs.HasKey("MyNickName"))
        {
            UserNickName = PlayerPrefs.GetString("MyNickName");
            return UserNickName;
        }
        return "";
    }
}
