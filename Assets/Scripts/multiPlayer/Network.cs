using UnityEngine;
using Unity.Netcode;

public class NetworkBootstrap : MonoBehaviour
{
    private void Awake()
    {
        NetworkManager nm = GetComponent<NetworkManager>();
        
        if (nm != null)
        {
            DontDestroyOnLoad(nm.gameObject);
        }
    }
}
