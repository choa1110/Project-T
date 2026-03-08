using UnityEngine;

public class BuffItem : MonoBehaviour
{
    [SerializeField] int buffNum;

    void OnTriggerEnter(Collider other)
    {
        BuffSystem sys = other.GetComponent<BuffSystem>();

        if (sys != null)
        {
            // BuffDB.Instance.ApplyItemBuffToPlayer(sys, buffNum);
            Destroy(gameObject);
        }
    }
}