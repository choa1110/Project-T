using UnityEngine;

public class Cannon : MonoBehaviour
{
    public GameObject Cannonball;
    public Transform shootPoint;
    public AttackParameters meters;

    [SerializeField] float term;

    float delta = 0f;

    void Update()
    {
        delta += Time.deltaTime;

        if (delta > term)
        {
            Shoot();
            delta = 0f;
        }
    }

    void Shoot()
    {
        GameObject go = Instantiate(Cannonball);
        go.transform.position = shootPoint.position;
        go.transform.forward = shootPoint.forward;

        AttackArea area = go.GetComponent<AttackArea>();
        area.SetAttackStatus(meters.parameters[0], 5, 25);
        area.AttackStart();

        Rigidbody rb = go.GetComponent<Rigidbody>();
        rb.AddForce(go.transform.forward * 25, ForceMode.Impulse);
    }
}