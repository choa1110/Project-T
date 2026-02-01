using System.Collections.Generic;
using UnityEngine;

public class ItemDB : MonoBehaviour
{
    public List<Item> itemList;

    public GameObject missile;

    public void UseItem(int itemID, Player user)
    {
        switch (itemID)
        {
            case 1:
                OnUse_HomingMissile(user);
                break;
            default:
                break;
        }
    }

    void OnUse_HomingMissile(Player user)
    {
        GameObject go = Instantiate(missile);

        Vector3 shootPosition = user.transform.position;
        shootPosition.y += 1f;
        shootPosition += user.transform.forward;

        go.transform.position = shootPosition;
        go.transform.forward = user.transform.forward;

        HomingMissile gm = go.GetComponent<HomingMissile>();
        gm.SetTarget(user.GetClosestOpponent());
    }
}