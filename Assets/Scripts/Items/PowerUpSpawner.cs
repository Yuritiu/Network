using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PowerUpSpawner : NetworkBehaviour
{
    [SerializeField] private List<Transform> powerUps = new List<Transform>();
    [SerializeField] private List<Transform> powerUpsSpawnPoints = new List<Transform>();

    [SerializeField] private float spawnMaxTimer = 10f;
    private float spawnTimer = 10f;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Only the server should drive the spawning logic
        if (!IsServer)
            return;

        spawnTimer = spawnMaxTimer;
    }

    private void Update()
    {
        // Only server runs the timer / spawns
        if (!IsServer)
            return;

        spawnTimer += Time.deltaTime;

        if (spawnTimer >= spawnMaxTimer)
        {
            if (powerUps.Count == 0 || powerUpsSpawnPoints.Count == 0)
            {
                Debug.LogWarning("PowerUpSpawner: no powerUps or spawn points assigned.");
                spawnTimer = 0f;
                return;
            }

            int powerUpIndex = Random.Range(0, powerUps.Count);
            int spawnIndex = Random.Range(0, powerUpsSpawnPoints.Count);

            // Tell everyone exactly which powerup + spawn index to use
            SpawnPowerupClientRpc(powerUpIndex, spawnIndex);

            spawnTimer = 0f;
        }
    }


    [ClientRpc]
    private void SpawnPowerupClientRpc(int powerUpIndex, int spawnIndex)
    {
        if (powerUps.Count == 0 || powerUpsSpawnPoints.Count == 0)
        {
            Debug.LogWarning("PowerUpSpawner: no powerUps or spawn points assigned.");
            return;
        }

        // Extra safety: clamp indexes (in case of mismatch)
        if (powerUpIndex < 0 || powerUpIndex >= powerUps.Count ||
            spawnIndex < 0 || spawnIndex >= powerUpsSpawnPoints.Count)
        {
            Debug.LogWarning($"PowerUpSpawner: received invalid indices p={powerUpIndex}, s={spawnIndex}");
            return;
        }

        Instantiate(
            powerUps[powerUpIndex],
            powerUpsSpawnPoints[spawnIndex].position,
            powerUps[powerUpIndex].rotation);
    }

}
