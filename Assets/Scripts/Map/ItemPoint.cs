using UnityEngine;

public class ItemPoint : MonoBehaviour
{
    public Vector3 pos => gameObject.transform.position;

    public bool isOccupied;
}