using System.Collections.Generic;
using UnityEngine;

public class ItemDB : MonoBehaviour
{
    static ItemDB _instance;
    public static ItemDB Instance { get => _instance; }

    [SerializeField] List<Item> itemList;

    public GameObject missile;

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

    public Item SetRandomItem()
    {
        int num = Random.Range(0, itemList.Count);

        return itemList[num];
    }

    void OnUse_HomingMissile(Player user)
    {
        GameObject go = Instantiate(missile);

        Vector3 shootPosition = user.transform.position;
        shootPosition.y += 1f;
        shootPosition += user.transform.forward;

        go.transform.position = shootPosition;
        go.transform.forward = user.transform.forward;

        HomingMissile hm = go.GetComponent<HomingMissile>();
        hm.SetTarget(GameManager.Instance.GetClosesetOpponent(user));
    }

    void OnUse_MagnetPull(Player user)
    {
        Player target = GameManager.Instance.GetClosesetOpponent(user);
        target.PulledToPoint(user);
    }
}