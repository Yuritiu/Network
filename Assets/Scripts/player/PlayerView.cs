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

        //enables camera
        if (playerCamera)
        {
            playerCamera.enabled = isMine;
            if (isMine)
            {
                playerCamera.tag = "MainCamera";
            }
        }

        //enables audio
        if (audioListener)
        {
            audioListener.enabled = isMine;
        }
        //enables UI
        if (playerCanvas)
        {
            playerCanvas.enabled = isMine;
        }
        //enables background
        if (screenBackground)
        {
            screenBackground.SetActive(isMine);
        }
    }
}
