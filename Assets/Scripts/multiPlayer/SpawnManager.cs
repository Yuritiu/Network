using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SpawnManager : NetworkBehaviour
{
    public static SpawnManager Instance { get; private set; }

    private readonly List<Transform> spawnPoints = new List<Transform>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void RegisterSpawnPoint(Transform spawnPoint)
    {
        if (!spawnPoints.Contains(spawnPoint))
        {
            spawnPoints.Add(spawnPoint);
        }
    }

    public void UnregisterSpawnPoint(Transform spawnPoint)
    {
        spawnPoints.Remove(spawnPoint);
    }

    public Vector3 GetSpawnPosition()
    {
        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning("SpawnManager: no spawn points registered, using Vector3.zero");
            return Vector3.zero;
        }

        int index = Random.Range(0, spawnPoints.Count);
        return spawnPoints[index].position;
    }
}
