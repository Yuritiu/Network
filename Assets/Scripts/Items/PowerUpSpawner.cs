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

        if (!IsServer)
        {
            return;
        }

        // reset the spawn timer
        spawnTimer = spawnMaxTimer;
    }

    private void Update()
    {
        if (!IsServer)
        {
            return;
        }

        spawnTimer += Time.deltaTime;

        // spawn when timer reaches max
        if (spawnTimer >= spawnMaxTimer)
        {
            // skip spawning if lists are empty
            if (powerUps.Count == 0 || powerUpsSpawnPoints.Count == 0)
            {
                spawnTimer = 0f;
                return;
            }

            // choose random powerup and spawn point
            int powerUpIndex = Random.Range(0, powerUps.Count);
            int spawnIndex = Random.Range(0, powerUpsSpawnPoints.Count);

            // tell all clients to spawn the powerup visually
            SpawnPowerupClientRpc(powerUpIndex, spawnIndex);

            // reset timer
            spawnTimer = 0f;
        }
    }


    [ClientRpc]
    private void SpawnPowerupClientRpc(int powerUpIndex, int spawnIndex)
    {
        if (powerUps.Count == 0 || powerUpsSpawnPoints.Count == 0)
        {
            return;
        }

        if (powerUpIndex < 0 || powerUpIndex >= powerUps.Count || spawnIndex < 0 || spawnIndex >= powerUpsSpawnPoints.Count)
        {
            return;
        }

        // spawn the powerup
        Instantiate(powerUps[powerUpIndex], powerUpsSpawnPoints[spawnIndex].position, powerUps[powerUpIndex].rotation);
    }

}
