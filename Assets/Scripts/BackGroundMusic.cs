using UnityEngine;

public class BackGroundMusic : MonoBehaviour
{
    static BackGroundMusic _instance;
    public static BackGroundMusic Instance { get => _instance; }

    [SerializeField] AudioSource bgmPlayer;

    [SerializeField] AudioClip lobbyBGM;
    [SerializeField] AudioClip battleBGM;

    [SerializeField] float volNormal;
    [SerializeField] float volDim;

    void Awake()
    {
        if (_instance == null)
            _instance = this;

        DontDestroyOnLoad(gameObject);

        bgmPlayer.generator = lobbyBGM;
        bgmPlayer.Play();
    }

    public void BattleOn()
    {
        bgmPlayer.generator = battleBGM;
        bgmPlayer.volume = volNormal;
        bgmPlayer.Play();
    }
    
    public void BackToLobby()
    {
        bgmPlayer.generator = lobbyBGM;
        bgmPlayer.volume = 1f;
        bgmPlayer.Play();
    }

    public void DimSound()
    {
        bgmPlayer.volume = volDim;
    }

    public void RaiseSound()
    {
        bgmPlayer.volume = volNormal;
    }
}