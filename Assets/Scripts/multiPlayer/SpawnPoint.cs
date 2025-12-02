using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    private void OnEnable()
    {
        if (SpawnManager.Instance != null)
        {
            SpawnManager.Instance.RegisterSpawnPoint(transform);
        }
    }

    private void OnDisable()
    {
        if (SpawnManager.Instance != null)
        {
            SpawnManager.Instance.UnregisterSpawnPoint(transform);
        }
    }
}
