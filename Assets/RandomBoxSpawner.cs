using UnityEngine;
using Fusion;

/// <summary>
/// Spawns RandomBox prefabs at random locations on the map at regular intervals.
/// </summary>
public class RandomBoxSpawner : NetworkBehaviour
{
    [Header("Prefab Settings")]
    [SerializeField] private NetworkPrefabRef _randomBoxPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private float _spawnInterval = 10f; // Seconds between spawns
    [SerializeField] private float _spawnHeight = 1f;   // Y-coordinate for spawning
    [SerializeField] private Vector2 _mapBoundsX = new Vector2(-15f, 15f); // Min/Max X position
    [SerializeField] private Vector2 _mapBoundsZ = new Vector2(-15f, 15f); // Min/Max Z position
    [SerializeField] private int _maxBoxes = 5;          // Maximum number of boxes allowed at once

    [Networked] private TickTimer _spawnTimer { get; set; }

    public override void Spawned()
    {
        // Only the State Authority (server/host) should handle spawning logic.
        if (Object.HasStateAuthority)
        {
            ResetTimer();
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority)
        {
            // Check if it's time to spawn a new box.
            if (_spawnTimer.Expired(Runner))
            {
                if (CanSpawn())
                {
                    SpawnRandomBox();
                }
                ResetTimer();
            }
        }
    }

    private void ResetTimer()
    {
        _spawnTimer = TickTimer.CreateFromSeconds(Runner, _spawnInterval);
    }

    private bool CanSpawn()
    {
        // Count existing boxes to prevent overcrowding.
        // We find all ItemBox instances currently active in the runner.
        int currentBoxCount = 0;
        foreach (var obj in Runner.GetAllNetworkObjects())
        {
            if (obj.GetComponent<ItemBox>() != null)
            {
                currentBoxCount++;
            }
        }
        return currentBoxCount < _maxBoxes;
    }

    private void SpawnRandomBox()
    {
        if (_randomBoxPrefab == null)
        {
            Debug.LogWarning("[RandomBoxSpawner] RandomBox prefab is not assigned!");
            return;
        }

        // Calculate a random position within the defined bounds.
        float randomX = Random.Range(_mapBoundsX.x, _mapBoundsX.y);
        float randomZ = Random.Range(_mapBoundsZ.x, _mapBoundsZ.y);
        Vector3 spawnPosition = new Vector3(randomX, _spawnHeight, randomZ);

        // Spawn the box. Since it's a networked object, it must be spawned via Runner.
        NetworkObject spawnedBox = Runner.Spawn(_randomBoxPrefab, spawnPosition, Quaternion.identity);
        
        // If ItemBox requires initialization (e.g., item range), do it here.
        ItemBox itemBox = spawnedBox.GetComponent<ItemBox>();
        if (itemBox != null)
        {
            // Set default range or customize as needed.
            itemBox.SetItemRange(1, 10); 
        }

        Debug.Log($"[RandomBoxSpawner] Spawned RandomBox at {spawnPosition}");
    }
}
