using UnityEngine;

public class StatBuffItem : MonoBehaviour
{
    public BuffData buffData;

    void OnTriggerEnter(Collider other)
    {
        BuffSystem sys = other.GetComponent<BuffSystem>();

        if (sys != null)
        {
            sys.ApplyBuff(buffData);
            Destroy(gameObject);
        }
    }
}