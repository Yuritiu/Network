using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class playerMovement : NetworkBehaviour
{
    [SerializeField] private Animator movementAnimator;
    //[SerializeField] private SpriteRenderer sprite;

    public GameObject spawnedObjectTransform;
    
    public float speed = 5f;
    private bool isMoving = false;
    private bool wasMovingLastFrame = false;
    private bool facingRight = false;

    public int score;

    public NetworkVariable<int> health = new NetworkVariable<int>( 3, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public void NetworkStart()
    {
        score = 0;
    }

    public override void OnNetworkSpawn()
    {
        //initializes health
        if (IsServer)
        {
            health.Value = 3;
        }
    }

    void Update()
    {
        if (IsOwner)
        {
            isMoving = false;

            if (Input.GetKey(KeyCode.RightArrow))
            {
                transform.position += new Vector3(speed * Time.deltaTime, 0f, 0f);
                isMoving = true;
                movementAnimator.SetBool("IsRunning", true);
                Flip(true);
            }
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                transform.position -= new Vector3(speed * Time.deltaTime, 0f, 0f);
                isMoving = true;
                movementAnimator.SetBool("IsRunning", true);
                Flip(false);
            }
            if (Input.GetKey(KeyCode.UpArrow))
            {
                transform.position += new Vector3(0f, speed * Time.deltaTime, 0f);
                isMoving = true;
                movementAnimator.SetBool("IsJump", true);
            }
            if (Input.GetKey(KeyCode.DownArrow))
            {
                transform.position -= new Vector3(0f, speed * Time.deltaTime, 0f);
                isMoving = true;
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                createBulletShotFromClientServerRpc(transform.position.x, transform.position.y, transform.position.z, transform.rotation, facingRight);
            }


            if (!isMoving && wasMovingLastFrame)
            {
                movementAnimator.SetBool("IsRunning", false);
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
    }

    void Flip(bool Dir)
    {
        if(Dir)
        {
            //sprite.flipX = false;
            transform.localScale = new Vector3(1, 1, 1);
            facingRight = false;
        }
        if(!Dir)
        {
            //sprite.flipX = true;
            transform.localScale = new Vector3(-1, 1, 1);
            facingRight = true;
        }
    }

    //called by bullet when hit
    public void TakeDamage(int amount)
    {
        if (!IsServer)
        {
            return;
        }

        health.Value = Mathf.Max(0, health.Value - amount);
        Debug.Log($"Player {OwnerClientId} took {amount} damage. Health = {health.Value}");

        if (health.Value <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"Player {OwnerClientId} died.");
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


            NetworkManager.Singleton.ConnectedClients[OwnerClientId].PlayerObject.GetComponent <playerMovement>().score =
            NetworkManager.Singleton.ConnectedClients[OwnerClientId].PlayerObject.GetComponent <playerMovement>().score + 1;
        }
        Debug.Log("the score of player " + targetClientId + " is " + NetworkManager.Singleton.ConnectedClients[OwnerClientId].PlayerObject.GetComponent<playerMovement>().score);
    }

    [ServerRpc]
    private void createBulletShotFromClientServerRpc(float positionx, float positiony, float positionz, Quaternion vector3rotation, bool facingRight)
    {
        float offsetX = 0.4f;
        float offsetY = 0.2f;

        if(facingRight)
        {
            offsetX *= -1;
        }
        GameObject spawnedObject = Instantiate(spawnedObjectTransform, new Vector3(positionx + offsetX, positiony - offsetY, positionz), vector3rotation).gameObject;
     
        Bullet bullet = spawnedObject.GetComponent<Bullet>();
        bullet.OwnerClientId = OwnerClientId;

        spawnedObject.GetComponent<Bullet>().SetDirection(facingRight ? Vector3.left : Vector3.right);
        spawnedObject.GetComponent<NetworkObject>().Spawn(true);
    }
}