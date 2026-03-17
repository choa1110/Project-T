using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class ItemDB : MonoBehaviour
{
    static ItemDB _instance;
    public static ItemDB Instance { get => _instance; }

    [SerializeField] List<Item> itemList;

    public NetworkPrefabRef missilePrefab;

    void Awake()
    {
        if (_instance == null)
            _instance = this;
    }

    public Item GetItemByID(int id)
    {
        return itemList.Find(x => x.itemId == id);
    }

    public int GetRandomItemID()
    {
        int num = Random.Range(0, itemList.Count);
        return itemList[num].itemId;
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

    void OnUse_HomingMissile(Player user)
    {
        Vector3 shootPosition = user.transform.position;
        shootPosition.y += 1f;
        shootPosition += user.transform.forward;

        NetworkObject go = user.Runner.Spawn(missilePrefab, shootPosition, Quaternion.LookRotation(user.transform.forward));
        go.transform.position = shootPosition;
        go.transform.forward = user.transform.forward;

        HomingMissile hm = go.GetComponent<HomingMissile>();
        if(hm !=null) hm.SetTarget(user.GetClosestOpponent());
    }

    void OnUse_MagnetPull(Player user)
    {
        Player target = user.GetClosestOpponent();
        if(target != null)
            target.PulledToPoint(user);
    }
}