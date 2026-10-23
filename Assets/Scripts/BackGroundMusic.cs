using Fusion;
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
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);

        bgmPlayer.generator = lobbyBGM;
        bgmPlayer.Play();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_BroadcastBattleOn()
    {
        bgmPlayer.generator = battleBGM;
        bgmPlayer.volume = volNormal;
        bgmPlayer.Play();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_BroadcastBackToLobby()
    {
        bgmPlayer.generator = lobbyBGM;
        bgmPlayer.volume = 1f;
        bgmPlayer.Play();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_BroadcastDimSound()
    {
        bgmPlayer.volume = volDim;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_BroadcastRaiseSound()
    {
        bgmPlayer.volume = volNormal;
    }

    public void BackToLobby()
    {
        bgmPlayer.generator = lobbyBGM;
        bgmPlayer.volume = 1f;
        bgmPlayer.Play();
    }

    public void RaiseSound()
    {
        bgmPlayer.volume = volNormal;
    }
}