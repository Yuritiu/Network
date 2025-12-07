using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using Unity.Collections;

public class playerManager : NetworkBehaviour
{
    [SerializeField] private Animator movementAnimator;
    [SerializeField] private Transform playerSprite;
    [SerializeField] private GameObject playerSpriteGO;
    [SerializeField] private GameObject Shield;

    [SerializeField] private Text nameText;

    public string playerName = "Player";

    public GameObject spawnedObjectTransform;

    public float speed = 5f;
    private bool isMoving = false;
    private bool wasMovingLastFrame = false;
    private bool facingLeft = false;

    private Rigidbody rb;

    [SerializeField] private float jumpForce;
    [SerializeField] private int maxJumps;   //double jump

    private int jumpsRemaining;
    private bool isGrounded = true;
    private int groundContacts = 0;

    [SerializeField] private float coyoteTime;  // 150ms grace to jump
    private float coyoteTimer = 0f;

    public int score;
    private int maxHealth = 2;
    private bool lowHealth = false;

    public NetworkVariable<int> health = new NetworkVariable<int>(3, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> isDead = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> facingRightNet = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    [SerializeField] private GameObject deathScreenUI;

    [SerializeField] private float shootCooldown = 1f;
    private float shootTimer = 0f;

    // Stats for leaderboard
    public NetworkVariable<int> kills = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> deaths = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private bool hasShield = false;
    private bool hasInvis = false;
    private bool hasGravity = false;

    private float baseDrag;
    [SerializeField] private float lowGravityDrag;

    public void NetworkStart()
    {
        score = 0;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // server decides health and (for late joiners) spawn position
            health.Value = maxHealth;

            if (SpawnManager.Instance != null)
            {
                transform.position = SpawnManager.Instance.GetSpawnPosition();
            }
        }

        rb = GetComponent<Rigidbody>();
        maxJumps = 1;
        jumpsRemaining = maxJumps;

        facingRightNet.OnValueChanged += OnFacingChanged;
        OnFacingChanged(false, facingRightNet.Value);

        if (IsOwner)
        {
            string localName = MainMenuUI.LocalPlayerName;
            if (string.IsNullOrWhiteSpace(localName))
            {
                localName = $"Player_{OwnerClientId}";
            }

            SubmitNameServerRpc(localName);
        }

        if (!string.IsNullOrEmpty(playerName) && nameText != null)
        {
            nameText.text = playerName;
        }

        isDead.OnValueChanged += OnDeathStateChanged;
        OnDeathStateChanged(false, isDead.Value);

        lowGravityDrag = -1f;

        if (rb != null)
        {
            baseDrag = rb.drag; // save original drag
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        facingRightNet.OnValueChanged -= OnFacingChanged;
        isDead.OnValueChanged -= OnDeathStateChanged;
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
            if (isDead.Value)
            {
                return;
            }

            isMoving = false;
            shootTimer += Time.deltaTime;

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
            if (Input.GetKeyDown(KeyCode.R) && shootTimer >= shootCooldown)
            {
                shootTimer = 0f;
                createBulletShotFromClientServerRpc(transform.position.x, transform.position.y, transform.position.z, transform.rotation, facingLeft);
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

        if (!IsOwner)
            return;

        if (target.gameObject.tag.Equals("Gravity") == true)
        {
            hasGravity = true;
            if (rb != null)
            {
                rb.drag = lowGravityDrag;
            }
            StartCoroutine(GravityTimer());
        }
        if (target.gameObject.tag.Equals("Invisibility") == true)
        {
            hasInvis = true;
            playerSpriteGO.GetComponent<Renderer>().material.color = new Color(1, 1, 1, 0.2f);
            StartCoroutine(InvisTimer());
        }
        if (target.gameObject.tag.Equals("Shield") == true)
        {
            hasShield = true;
            ShieldSpriteServerRpc(true);
            StartCoroutine(ShieldTimer());
        }
    }

    IEnumerator GravityTimer()
    {
        yield return new WaitForSeconds(5);
        if (rb != null)
        {
            rb.drag = baseDrag;
        }
        hasGravity = false;
    }
    IEnumerator InvisTimer()
    {
        yield return new WaitForSeconds(5);
        hasInvis = false;
        if (lowHealth)
        {
            LowHealthServerRpc();
        }
        else
        {
            playerSpriteGO.GetComponent<Renderer>().material.color = new Color(1, 1, 1, 1);
        }
    }
    IEnumerator ShieldTimer()
    {
        yield return new WaitForSeconds(10);
        hasShield = false;
        ShieldSpriteServerRpc(false);
    }

    void OnCollisionStay(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Ground"))
            return;

        foreach (var contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                isGrounded = true;
                jumpsRemaining = maxJumps;

                if (rb != null && rb.velocity.y <= 0.05f)
                {
                    movementAnimator.SetBool("IsJump", false);
                }

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
            facingLeft = false;
        }
        else
        {
            playerSprite.localScale = new Vector3(-3f, 3f, 3f);    // facing left
            facingLeft = true;
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
    public void TakeDamage(int amount, ulong attackerClientId = ulong.MaxValue)
    {
        if (!IsServer)
        {
            return;
        }
        if (hasShield)
        {
            hasShield = false;
            ShieldSpriteServerRpc(false);
            return;
        }

        health.Value = Mathf.Max(0, health.Value - amount);
        Debug.Log($"Player {OwnerClientId} took {amount} damage. Health = {health.Value}");

        if (health.Value == 1)
        {
            LowHealthServerRpc();
            lowHealth = true;
        }

        if (health.Value <= 0)
        {
            // Count deaths for this player
            deaths.Value++;

            // Count kill for attacker (if it's a valid other player)
            if (attackerClientId != ulong.MaxValue && attackerClientId != OwnerClientId)
            {
                if (NetworkManager.Singleton.ConnectedClients.TryGetValue(attackerClientId, out var attackerClient))
                {
                    playerManager attackerPm = attackerClient.PlayerObject.GetComponent<playerManager>();
                    if (attackerPm != null)
                    {
                        attackerPm.kills.Value++;
                        Debug.Log($"Player {attackerClientId} scored a kill. Total kills = {attackerPm.kills.Value}");
                    }
                }
            }

            Die();
        }
    }

    private void Die()
    {
        if (isDead.Value)
            return;

        isDead.Value = true;

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
        }
    }

    private void OnDeathStateChanged(bool oldValue, bool newValue)
    {
        if (newValue)
        {
            // became dead
            movementAnimator.SetBool("IsRunning", false);
            movementAnimator.SetBool("IsJump", false);

            // only the local owner sees their death screen
            if (IsOwner && deathScreenUI != null)
            {
                deathScreenUI.SetActive(true);
            }
            if (playerSpriteGO != null)
            {
                playerSpriteGO.SetActive(false);
            }
        }
        else
        {
            // respawned
            if (IsOwner && deathScreenUI != null)
            {
                deathScreenUI.SetActive(false);
            }
            if (playerSpriteGO != null)
            {
                playerSpriteGO.SetActive(true);
            }
            lowHealth = false;
        }
    }

    public void OnRespawnButtonPressed()
    {
        if (!IsOwner)
            return;

        RequestRespawnServerRpc();
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


            NetworkManager.Singleton.ConnectedClients[OwnerClientId].PlayerObject.GetComponent<playerManager>().score =
            NetworkManager.Singleton.ConnectedClients[OwnerClientId].PlayerObject.GetComponent<playerManager>().score + 1;
        }
        Debug.Log("the score of player " + targetClientId + " is " + NetworkManager.Singleton.ConnectedClients[OwnerClientId].PlayerObject.GetComponent<playerManager>().score);
    }

    [ServerRpc]
    private void createBulletShotFromClientServerRpc(float positionx, float positiony, float positionz, Quaternion vector3rotation, bool facingRight)
    {
        float offsetX = 0.6f;
        float offsetY = 0.2f;

        if (facingRight)
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

    [ServerRpc]
    private void RequestRespawnServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!isDead.Value)
            return; // ignore if somehow not dead

        // server chooses spawn position
        Vector3 spawnPos = Vector3.zero;
        if (SpawnManager.Instance != null)
        {
            spawnPos = SpawnManager.Instance.GetSpawnPosition();
        }

        health.Value = maxHealth;
        isDead.Value = false;

        // tell the owner client to move itself
        RespawnClientRpc(spawnPos, new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { OwnerClientId }
            }
        });
    }

    [ClientRpc]
    private void RespawnClientRpc(Vector3 spawnPos, ClientRpcParams clientRpcParams = default)
    {
        transform.position = spawnPos;

        if (playerSpriteGO != null)
        {
            var rend = playerSpriteGO.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = new Color(1, 1, 1, 1);
            }
        }
    }


    [ServerRpc]
    private void LowHealthServerRpc()
    {
        LowHealthClientRpc();
    }

    [ClientRpc]
    private void LowHealthClientRpc()
    {
        playerSpriteGO.GetComponent<Renderer>().material.color = new Color(1, 0.65f, 0.65f);
    }

    [ServerRpc]
    private void ShieldSpriteServerRpc(bool isActive)
    {
        ShieldSpriteClientRpc(isActive);
    }

    [ClientRpc]
    private void ShieldSpriteClientRpc(bool isActive)
    {
        Shield.SetActive(isActive);
    }
}