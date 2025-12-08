using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SpawnManager : NetworkBehaviour
{
    public static SpawnManager Instance;

    // list of available spawn points
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // only server handles respawning connected players
        if (!IsServer)
        {
            return;
        }

        // reposition all connected players when scene loads as a network scene
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            playerManager player = null;

            // gets player manager component from the client’s player object
            if (client.PlayerObject != null)
            {
                player = client.PlayerObject.GetComponent<playerManager>();
            }

            // if a valid player was found move them to a spawn point
            if (player != null)
            {
                player.transform.position = GetSpawnPosition();
            }
        }
    }

    public void RegisterSpawnPoint(Transform spawnPoint)
    {
        // add spawn point if not already in the list
        if (!spawnPoints.Contains(spawnPoint))
        {
            spawnPoints.Add(spawnPoint);
        }
    }

    public void UnregisterSpawnPoint(Transform spawnPoint)
    {
        // remove spawn point from the list
        spawnPoints.Remove(spawnPoint);
    }

    public Vector3 GetSpawnPosition()
    {
        // return zero if no spawn points are registered
        if (spawnPoints.Count == 0)
        {
            return Vector3.zero;
        }

        // pick a random spawn point and return its position
        int index = Random.Range(0, spawnPoints.Count);
        return spawnPoints[index].position;
    }
}
