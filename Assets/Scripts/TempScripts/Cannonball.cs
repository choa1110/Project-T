using UnityEngine;

public class Cannonball : MonoBehaviour
{
    AttackArea _area;

    void Awake()
    {
        _area = GetComponent<AttackArea>();
        _area.SetOwner(gameObject);

        Destroy(gameObject, 5f);
    }
}