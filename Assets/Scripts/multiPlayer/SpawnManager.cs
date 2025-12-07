using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SpawnManager : NetworkBehaviour
{
    public static SpawnManager Instance { get; private set; }

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

        if (!IsServer)
            return;

        // When the Network scene is loaded as a network scene, reposition all current players.
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var player = client.PlayerObject != null
                ? client.PlayerObject.GetComponent<playerManager>()
                : null;

            if (player != null)
            {
                player.transform.position = GetSpawnPosition();
            }
        }
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
