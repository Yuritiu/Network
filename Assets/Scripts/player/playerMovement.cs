using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using Unity.Collections;

public class playerMovement : NetworkBehaviour
{
    [SerializeField] private Animator movementAnimator;
    [SerializeField] private Transform playerSprite;
    //[SerializeField] private SpriteRenderer sprite;

    [SerializeField] private Text nameText;

    public string playerName = "Player";

    public GameObject spawnedObjectTransform;
    
    public float speed = 5f;
    private bool isMoving = false;
    private bool wasMovingLastFrame = false;
    private bool facingRight = false;

    private Rigidbody rb;

    [SerializeField] private float jumpForce;
    [SerializeField] private int maxJumps;   //double jump
    
    private int jumpsRemaining;
    private bool isGrounded = true;
    private int groundContacts = 0;

    [SerializeField] private float coyoteTime;  // 150ms grace to jump
    private float coyoteTimer = 0f;

    public int score;

    public NetworkVariable<int> health = new NetworkVariable<int>( 3, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> facingRightNet = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public void NetworkStart()
    {
        score = 0;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            health.Value = 3;
        }

        rb = GetComponent<Rigidbody>();
        jumpsRemaining = maxJumps;

        facingRightNet.OnValueChanged += OnFacingChanged;
        OnFacingChanged(false, facingRightNet.Value);

        // OWNER tells the server their name once
        if (IsOwner)
        {
            string localName = MainMenuUI.LocalPlayerName;
            if (string.IsNullOrWhiteSpace(localName))
            {
                localName = $"Player_{OwnerClientId}";
            }

            SubmitNameServerRpc(localName);
        }

        // no NetworkVariable name subscription now; just set whatever we currently have
        if (!string.IsNullOrEmpty(playerName) && nameText != null)
        {
            nameText.text = playerName;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        facingRightNet.OnValueChanged -= OnFacingChanged;
    }

    private void OnNameChanged(FixedString64Bytes oldName, FixedString64Bytes newName)
    {
        if (nameText != null)
        {
            nameText.text = newName.ToString();
        }
    }

    void Update()
    {
        if (IsOwner)
        {
            isMoving = false;

            // coyote timer
            if (isGrounded)
            {
                coyoteTimer = coyoteTime; //reset timer while grounded
            }
            else
            {
                coyoteTimer -= Time.deltaTime; //count down after leaving ground
            }

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
            if (Input.GetKeyDown(KeyCode.UpArrow) && (coyoteTimer > 0f || jumpsRemaining > 0))
            {
                if (rb != null)
                {
                    rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
                    rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
                }

                jumpsRemaining--;
                isGrounded = false;
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
            isGrounded = true;
            jumpsRemaining = maxJumps;
            movementAnimator.SetBool("IsJump", false);
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Ground"))
            return;

        foreach (var contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                groundContacts++;
                isGrounded = true;
                jumpsRemaining = maxJumps;
                movementAnimator.SetBool("IsJump", false);
                break;
            }
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Ground"))
            return;

        groundContacts = Mathf.Max(0, groundContacts - 1);
        if (groundContacts == 0)
        {
            isGrounded = false;
        }
    }

    private void OnFacingChanged(bool oldValue, bool newValue)
    {
        if (playerSprite == null)
        {
            return;
        }

        if (newValue)
        {
            playerSprite.localScale = new Vector3(3f, 3f, 3f);    // facing right
        }
        else
        {
            playerSprite.localScale = new Vector3(-3f, 3f, 3f);    // facing left
        }
    }

    void Flip(bool Dir)
    {
        if (facingRightNet.Value != Dir)
        {
            facingRightNet.Value = Dir;   // this will trigger OnFacingChanged on all clients
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

    [ServerRpc]
    private void SubmitNameServerRpc(string name, ServerRpcParams rpcParams = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            name = $"Player_{OwnerClientId}";
        }

        // server stores the name on this player
        playerName = name;

        // broadcast to all clients so they update their local copy & UI
        ApplyNameClientRpc(name);
    }

    [ClientRpc]
    private void ApplyNameClientRpc(string name)
    {
        playerName = name;

        if (nameText != null)
        {
            nameText.text = name;
        }
    }
}