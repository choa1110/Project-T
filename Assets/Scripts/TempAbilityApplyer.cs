using UnityEngine;

public class TempAbilityApplyer : MonoBehaviour
{
    public Ability ability;

    void OnTriggerEnter(Collider other)
    {
        Player sys = other.GetComponent<Player>();

        if (sys != null)
        {
            sys.SetAbility(ability);
            Destroy(gameObject);
        }
    }
}
