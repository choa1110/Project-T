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

        Runner.Spawn(missile, shootPosition, user.transform.rotation, Runner.LocalPlayer, (runner, obj) => {
            if (obj.TryGetBehaviour<HomingMissile>(out var missileBehaviour))
            {
                missileBehaviour.SetOwner(user);
                missileBehaviour.SetTarget(GameManager.Instance.GetClosesetOpponent(user));
            }
        });
    }

    void OnUse_MagnetPull(Player user)
    {
        Player target = GameManager.Instance.GetClosesetOpponent(user);
        
        target.PulledToPoint(user);
    }

    // Rpc Request
}