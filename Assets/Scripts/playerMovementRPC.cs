using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.PlayerLoop;
using UnityEngine.SocialPlatforms.Impl;

public class playerMovementRPC : NetworkBehaviour
{
    [SerializeField] private Animator movementAnimator;
    //[SerializeField] private SpriteRenderer sprite;

    public float speed = 0.002f;
    private bool isMoving = false;
    private bool wasMovingLastFrame = false;
    public int theScore;

    public void NetworkStart()
    {
        theScore = 0;
    }

    public override void OnNetworkSpawn()
    {
        //movementAnimator = this.GetComponent<Animator>();
    }

    
    void Update()
    {
        if (IsOwner)
        {
            isMoving = false;
            
            if (Input.GetKey(KeyCode.RightArrow))
            {
                HandleMovementLocally(1);
                HandleMovementServerRpc(1, this.NetworkObjectId);
                isMoving = true;
            }
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                HandleMovementLocally(2);
                HandleMovementServerRpc(2, this.NetworkObjectId);
                isMoving = true;
            }
            if (Input.GetKey(KeyCode.UpArrow))
            {
                HandleMovementLocally(3);
                HandleMovementServerRpc(3, this.NetworkObjectId);
                isMoving = true;
            }
            if (Input.GetKey(KeyCode.DownArrow))
            {
                HandleMovementLocally(4);
                HandleMovementServerRpc(4, this.NetworkObjectId);
                isMoving = true;
            }

            if (!isMoving && wasMovingLastFrame)
            {
                HandleMovementLocally(5);
                HandleMovementServerRpc(5, this.NetworkObjectId);
            }

            wasMovingLastFrame = isMoving;
        }
    }
    
    void OnCollisionEnter(Collision target)
    {
        if (target.gameObject.tag.Equals("Ground") == true)
        {
            movementAnimator.SetBool("IsJump", false);
        }
        if (target.gameObject.tag.Equals("Coin") == true)
        {
            if (NetworkManager.Singleton.LocalClientId == OwnerClientId)
            {
                scoreCollectedServerRpc(OwnerClientId);
            }
        }
    }

    void Flip(bool Dir)
    {
        if(Dir)
        {
            //sprite.flipX = false;
            transform.localScale = new Vector3(1, 1, 1);
        }
        if(!Dir)
        {
            //sprite.flipX = true;
            transform.localScale = new Vector3(-1, 1, 1);
        }
    }

    [ServerRpc]
    void HandleMovementServerRpc(int movementDirection, ulong senderClientId)
    {
        //Debug.Log("the player " + senderClientId + " just moves from position " +
        //    NetworkManager.Singleton.ConnectedClients[senderClientId - 1].PlayerObject.transform.position);

        HandleMovementClientRpc(movementDirection, senderClientId);
    }

    [ClientRpc]
    void HandleMovementClientRpc(int movementDirection, ulong senderClientId)
    {
        HandleMovementLocally(movementDirection);
    }

    void HandleMovementLocally(int movementDirection)
    {
        switch (movementDirection)
        {
            case 1:
                transform.position += new Vector3(speed * Time.deltaTime, 0f, 0f);
                movementAnimator.SetBool("IsRunning", true);
                Flip(true);
                break;

            case 2:
                transform.position -= new Vector3(speed * Time.deltaTime, 0f, 0f);
                movementAnimator.SetBool("IsRunning", true);
                Flip(false);
                break;

            case 3:
                transform.position += new Vector3(0f, speed * Time.deltaTime, 0f);
                movementAnimator.SetBool("IsJump", true);
                break;

            case 4:
                transform.position -= new Vector3(0f, speed * Time.deltaTime, 0f);
                break;

            case 5:
                movementAnimator.SetBool("IsRunning", false);
                break;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void scoreCollectedServerRpc(ulong clientId)
    {
        //request the players to send all their scores
        coinCollectedClientRpc(clientId);
    }


    [ClientRpc]
    private void coinCollectedClientRpc(ulong targetClientId)
    {
        //get the TargetClientID, compare it to the owner id and if the same update the score
        if (targetClientId == OwnerClientId)
        {
            //theClientObject = this.gameObject;

            NetworkManager.Singleton.ConnectedClients[OwnerClientId].PlayerObject.GetComponent
            <playerMovementRPC>().theScore =
            NetworkManager.Singleton.ConnectedClients[OwnerClientId].PlayerObject.GetComponent
            <playerMovementRPC>().theScore + 1;
        }
        Debug.Log("the score of player " + targetClientId + " is " +
       NetworkManager.Singleton.ConnectedClients[OwnerClientId].PlayerObject.GetComponent
       <playerMovementRPC>().theScore);
    }
}