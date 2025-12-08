using UnityEngine;
using Unity.Netcode;

public class NetworkBootstrap : MonoBehaviour
{
    private void Awake()
    {
        NetworkManager networkManager = GetComponent<NetworkManager>();
        
        if (networkManager != null)
        {
            DontDestroyOnLoad(networkManager.gameObject);
        }
    }
}
