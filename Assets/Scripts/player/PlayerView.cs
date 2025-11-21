using Unity.Netcode;
using UnityEngine;

public class PlayerView : NetworkBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private AudioListener audioListener; 
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

        if (audioListener)
        {
            audioListener.enabled = isMine;
        }
        if (playerCanvas)
        {
            playerCanvas.enabled = isMine;
        }
        if (screenBackground)
        {
            screenBackground.SetActive(isMine);
        }
    }
}
