using UnityEngine;

public class DataManager : MonoBehaviour
{
    static DataManager _instance;
    public static DataManager Instance { get => _instance; }

    public string UserNickName { get; private set; }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            LoadNickName();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void SetNickName(string name)
    {
        UserNickName = name;
        PlayerPrefs.SetString("MyNickName", name);
        PlayerPrefs.Save();
        Debug.Log($"[DataManager] Nickname saved: {name}");
    }

    public string LoadNickName()
    {
        if (PlayerPrefs.HasKey("MyNickName"))
        {
            UserNickName = PlayerPrefs.GetString("MyNickName");
            Debug.Log($"[DataManager] Nickname loaded: {UserNickName}");
            return UserNickName;
        }
        UserNickName = "";
        return "";
    }
}