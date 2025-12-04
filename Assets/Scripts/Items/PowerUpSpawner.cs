using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class PowerUpSpawner : NetworkBehaviour
{
    [SerializeField] private List<Transform> powerUps = new List<Transform>();
    [SerializeField] private List<Transform> powerUpsSpawnPoints = new List<Transform>();

    [SerializeField] private float spawnMaxTimer = 10f;
    private float spawnTimer = 10f;

    public override void OnNetworkSpawn()
    {
        spawnMaxTimer = 10f;
        spawnTimer = 10f;
    }

    private void Update()
    {
        spawnTimer += Time.deltaTime;

        if(spawnTimer >= spawnMaxTimer)
        {
            SpawnPowerupServerRpc();
            spawnTimer = 0;
        }
    }

    [ServerRpc]
    private void SpawnPowerupServerRpc()
    {
        SpawnPowerupClientRpc();
    }

    [ClientRpc]
    private void SpawnPowerupClientRpc()
    {
        int powerUp = Random.Range(0, powerUps.Count);
        int spawn = Random.Range(0, powerUpsSpawnPoints.Count);

        GameObject spawnedObject = Instantiate(powerUps[powerUp], powerUpsSpawnPoints[spawn]).gameObject;
    }
}
