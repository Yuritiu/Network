using Unity.Netcode;
using UnityEngine;

public class PlayerView : NetworkBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Canvas playerCanvas; 
    [SerializeField] private GameObject screenBackground;

    public override void OnNetworkSpawn()
    {
        bool isMine = IsOwner;

        if (playerCamera)
        {
            playerCamera.enabled = isMine;
            if (isMine)
            {
                playerCamera.tag = "MainCamera";
            }
        }

        if (playerCanvas) playerCanvas.enabled = isMine;
        if (screenBackground) screenBackground.SetActive(isMine);
    }
}
