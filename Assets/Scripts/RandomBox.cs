using UnityEngine;
using Fusion;

public class RandomBox : NetworkBehaviour
{
    [SerializeField] NetworkObject _networkObj;

    [SerializeField] int from, to;

    float _initalHeight;
    ItemPoint spawn;

    public void SetSpawnPoint(ItemPoint point)
    {
        spawn = point;
        spawn.isOccupied = true;

        _initalHeight = spawn.pos.y + 1;
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
        if (Object && !Object.HasStateAuthority) return;

        if (other.TryGetComponent<ItemSystem>(out ItemSystem sys))
        {
            if (sys.SetItem(ItemDB.Instance.GetItem(Random.Range(from, to))))
            {
                spawn.isOccupied = false;
                Runner.Despawn(_networkObj);
            }
        }
    }
}