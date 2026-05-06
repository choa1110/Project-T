using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class ItemDB : NetworkBehaviour
{
    static ItemDB _instance;
    public static ItemDB Instance { get => _instance; }

    [SerializeField] List<Item> itemList;

    public NetworkObject missile;

    void Awake()
    {
        if (_instance == null)
            _instance = this;
    }

    public void UseItem(int itemID, Player user)
    {
        switch (itemID)
        {
            case 1:
                OnUse_HomingMissile(user);
                break;
            case 2:
                OnUse_MagnetPull(user);
                break;
            default:
                break;
        }
    }

    public Item GetItem(int num)
    {
        if (num < 0 || num > itemList.Count - 1)
            return null;

        return itemList[num];
    }

    void OnUse_HomingMissile(Player user)
    {
        Vector3 shootPosition = user.transform.position;
        shootPosition.y += 1f;
        shootPosition += user.transform.forward;

        Rpc_RequestMissileToServer(shootPosition, user.transform.rotation, GameManager.Instance.GetClosestOpponent(user));
    }

    void OnUse_MagnetPull(Player user)
    {
        Player target = GameManager.Instance.GetClosestOpponent(user);

        Rpc_RequestMagnetToServer(user, target);
    }

    // Rpc Request
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    void Rpc_RequestMissileToServer(Vector3 position, Quaternion rotation, Player target)
    {
        Runner.Spawn(missile, position, rotation, Runner.LocalPlayer, (runner, obj) => {
            if (obj.TryGetBehaviour<HomingMissile>(out var missile))
                missile.SetTarget(target);
        });
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    void Rpc_RequestMagnetToServer(Player user, Player target)
    {
        if (target != null)
            target.PulledToPoint(user);
    }
}
