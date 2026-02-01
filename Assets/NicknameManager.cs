using UnityEngine;

public class NicknameManager : MonoBehaviour
{
    public static NicknameManager Instance;

    //나중에 스팀 연동하면 이 변수만 true로
    public bool isSteamMode = false; 

    private string _myNickname;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 유지
    }

    public string GetMyNickname()
    {
        //이미 저장된 닉네임이 있으면 반환
        if (!string.IsNullOrEmpty(_myNickname)) return _myNickname;

        //스팀 모드라면 스팀에서 가져오기(연결 후 주석 해제)
        if (isSteamMode)
        {
            // _myNickname = Steamworks.SteamFriends.GetPersonaName();
            // return _myNickname;
        }

        //테스트 모드라면 로컬 저장소(PlayerPrefs)에서 불러오기
        _myNickname = PlayerPrefs.GetString("UserNickName", "Guest_" + Random.Range(1000, 9999));
        return _myNickname;
    }

    public void SetNickname(string name)
    {
        _myNickname = name;
        PlayerPrefs.SetString("UserNickName", name); // 로컬에 저장
        PlayerPrefs.Save();
    }
}