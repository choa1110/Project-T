using UnityEngine;
using Fusion;

public class RandomBox : NetworkBehaviour
{
    [SerializeField] NetworkObject _networkObj;

    [SerializeField] int from, to;

    float _initalHeight;

    public override void Spawned()
    {
        _initalHeight = transform.position.y;
    }

    public void SetItemRange(int start, int end)
    {
        from = start; to = end;
    }

    public override void FixedUpdateNetwork()
    {
        float sineValue = Mathf.Sin(Runner.SimulationTime * 2f) * 0.1f;

        transform.position = new Vector3(transform.position.x, _initalHeight + sineValue, transform.position.z);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!Object.HasStateAuthority) return;

        if (other.TryGetComponent<ItemSystem>(out ItemSystem sys))
        {
            if (sys.SetItem(ItemDB.Instance.GetItem(Random.Range(from, to))))
                Runner.Despawn(_networkObj);
        }
    }
}