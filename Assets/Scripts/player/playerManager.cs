using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using Unity.Collections;

public class playerManager : NetworkBehaviour
{
    //Sprites
    [SerializeField] private Animator movementAnimator;
    [SerializeField] private Transform playerSprite;
    [SerializeField] private GameObject playerSpriteGO;

    //Name
    [SerializeField] private Text nameText;
    public string playerName = "Player";

    //movement
    private NetworkVariable<bool> facingRightNet = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    
    public float speed = 5f;
    private bool isMoving = false;
    private bool wasMovingLastFrame = false;
    private bool facingLeft = false;

    private Rigidbody rb;

    [SerializeField] private float jumpForce;
    [SerializeField] private int maxJumps; //double jump

    private int jumpsRemaining;
    private bool isGrounded = true;
    private int groundContacts = 0;

    [SerializeField] private float coyoteTime; // 150ms grace to jump
    private float coyoteTimer = 0f;

    //health and death
    [SerializeField] private GameObject deathScreenUI;
    
    public int score;
    private int maxHealth = 2;
    private bool lowHealth = false;

    public NetworkVariable<int> health = new NetworkVariable<int>(3, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> isDead = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    


    //Shooting varibles
    public GameObject bullet;

    [SerializeField] private float shootCooldown = 1f;
    private float shootTimer = 0f;

    //stats for leaderboard
    public NetworkVariable<int> kills = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> deaths = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    //Power Ups
    private bool hasShield = false;
    private bool hasInvis = false;
    private bool hasGravity = false;

    [SerializeField] private GameObject Shield;
    private Coroutine shieldCoroutine;

    //slow fall varibles
    [SerializeField] private float lowGravityDrag;
    private float baseDrag;

    public void NetworkStart()
    {
        score = 0;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // server decides health and (for late clients) spawn position
            health.Value = maxHealth;

            if (SpawnManager.Instance != null)
            {
                transform.position = SpawnManager.Instance.GetSpawnPosition();
            }
        }

        rb = GetComponent<Rigidbody>();
        maxJumps = 1;
        jumpsRemaining = maxJumps;

        //ensures player is facing the right way
        facingRightNet.OnValueChanged += OnFacingChanged;
        OnFacingChanged(false, facingRightNet.Value);

        if (IsOwner)
        {
            //sets name
            string localName = MainMenuUI.LocalPlayerName;

            SubmitNameServerRpc(localName);
        }

        if (!string.IsNullOrEmpty(playerName) && nameText != null)
        {
            //sets leaderboard name
            nameText.text = playerName;
        }

        //sets death UI 
        isDead.OnValueChanged += OnDeathStateChanged;
        OnDeathStateChanged(false, isDead.Value);

        //sets slow fall speed
        lowGravityDrag = -1f;

        if (rb != null)
        {
            //save original drag
            baseDrag = rb.drag; 
        }
    }

    public override void OnNetworkDespawn()
    {

        base.OnNetworkDespawn();
        //stops player functions being called 
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

            //resets animation
            isMoving = false;

            //countdown for shooting
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

            //move right
            if (Input.GetKey(KeyCode.D))
            {
                transform.position += new Vector3(speed * Time.deltaTime, 0f, 0f);
                isMoving = true;
                movementAnimator.SetBool("IsRunning", true);
                Flip(true);
            }
            //move left
            if (Input.GetKey(KeyCode.A))
            {
                transform.position -= new Vector3(speed * Time.deltaTime, 0f, 0f);
                isMoving = true;
                movementAnimator.SetBool("IsRunning", true);
                Flip(false);
            }
            //jump
            if (Input.GetKeyDown(KeyCode.W) && (coyoteTimer > 0f || jumpsRemaining > 0))
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
            //move down
            if (Input.GetKey(KeyCode.S))
            {
                transform.position -= new Vector3(0f, speed * Time.deltaTime, 0f);
                isMoving = true;
            }
            //shoot
            if (Input.GetKeyDown(KeyCode.Space) && shootTimer >= shootCooldown)
            {
                shootTimer = 0f;
                createBulletShotFromClientServerRpc(transform.position.x, transform.position.y, transform.position.z, transform.rotation, facingLeft);
            }


            //stops animation
            if (!isMoving && wasMovingLastFrame)
            {
                movementAnimator.SetBool("IsRunning", false);
            }

            //stops resetting the animation every tick
            wasMovingLastFrame = isMoving;
        }
    }

    void OnCollisionEnter(Collision target)
    {
        //resets the jumps and animation
        if (target.gameObject.tag.Equals("Ground") == true)
        {
            isGrounded = true;
            jumpsRemaining = maxJumps;
            movementAnimator.SetBool("IsJump", false);
        }

        if (!IsOwner)
        {
            return;
        }

        //pickup gravity power
        if (target.gameObject.tag.Equals("Gravity") == true)
        {
            hasGravity = true;
            if (rb != null)
            {
                //changed gravity
                rb.drag = lowGravityDrag;
            }
            StartCoroutine(GravityTimer());
        }
        //pickup invis power
        if (target.gameObject.tag.Equals("Invisibility") == true && IsOwner)
        {
            StartCoroutine(InvisTimer());

        }
        //pickup shield power
        if (target.gameObject.tag.Equals("Shield") == true)
        {
            PickupShieldServerRpc();
        }
    }

    IEnumerator GravityTimer()
    {
        yield return new WaitForSeconds(5);
        //resets gravity
        if (rb != null)
        {
            rb.drag = baseDrag;
        }
        hasGravity = false;
    }
    IEnumerator InvisTimer()
    {
        //sets sprite alpha low
        SetInvisibleServerRpc(true);

        yield return new WaitForSeconds(5);

        //resets alpha
        hasInvis = false;
        if (lowHealth)
        {
            LowHealthServerRpc();
        }
        else
        {
            SetInvisibleServerRpc(false);
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Ground"))
        {
            return;
        }

        //adds more reliability to these variables
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
        {
            return;
        }

        //update grounded state.
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
            // facing right
            playerSprite.localScale = new Vector3(3f, 3f, 3f);    
            facingLeft = false;
        }
        else
        {
            // facing left
            playerSprite.localScale = new Vector3(-3f, 3f, 3f);    
            facingLeft = true;
        }
    }

    void Flip(bool Dir)
    {
        if (facingRightNet.Value != Dir)
        {
            //this will trigger OnFacingChanged on all clients
            facingRightNet.Value = Dir;   
        }
    }

    //called by bullet when player is hit
    public void TakeDamage(int amount, ulong attackerClientId = ulong.MaxValue)
    {
        if (!IsServer)
        {
            return;
        }
        if (hasShield)
        {
            //shield stops damage
            hasShield = false;
            ShieldSpriteClientRpc(false);
            return;
        }

        health.Value = Mathf.Max(0, health.Value - amount);

        if (health.Value == 1)
        {
            LowHealthClientRpc();
            lowHealth = true;
        }

        if (health.Value <= 0)
        {
            //count deaths for this player
            deaths.Value++;

            //count kill for attacker (if it's a valid other player)
            if (attackerClientId != ulong.MaxValue && attackerClientId != OwnerClientId)
            {
                if (NetworkManager.Singleton.ConnectedClients.TryGetValue(attackerClientId, out var attackerClient))
                {
                    playerManager attackerPlayerManager = attackerClient.PlayerObject.GetComponent<playerManager>();
                    if (attackerPlayerManager != null)
                    {
                        attackerPlayerManager.kills.Value++;
                    }
                }
            }

            Die();
        }
    }

    private void Die()
    {
        //ignore if already dead
        if (isDead.Value)
        {
            return;
        }

        //mark player as dead
        isDead.Value = true;

        //stop all movement
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
        }
    }

    private void OnDeathStateChanged(bool oldValue, bool newValue)
    {
        if (newValue)
        {
            // dead
            movementAnimator.SetBool("IsRunning", false);
            movementAnimator.SetBool("IsJump", false);

            // only the local owner sees their death screen
            if (IsOwner && deathScreenUI != null)
            {
                deathScreenUI.SetActive(true);
            }

            //removes for all players
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
                
                var rend = playerSpriteGO.GetComponent<Renderer>();
                if (rend != null)
                {
                    //removes red tint
                    rend.material.color = new Color(1f, 1f, 1f, 1f);
                }
            }
            lowHealth = false;
        }
    }

    public void OnRespawnButtonPressed()
    {
        if (!IsOwner)
        { 
            return;
        }

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
    }

    [ServerRpc]
    private void createBulletShotFromClientServerRpc(float positionx, float positiony, float positionz, Quaternion vector3rotation, bool facingRight)
    {
        //apply small positional offsets for bullet spawn
        float offsetX = 0.6f;
        float offsetY = 0.2f;

        //flip horizontal offset if facing right
        if (facingRight)
        {
            offsetX *= -1;
        }

        //spawn the bullet on the server at the adjusted position
        GameObject spawnedObject = Instantiate(this.bullet, new Vector3(positionx + offsetX, positiony - offsetY, positionz), vector3rotation).gameObject;

        //assign bullet ownership
        Bullet bullet = spawnedObject.GetComponent<Bullet>();
        bullet.OwnerClientId = OwnerClientId;

        //set bullet travel direction
        spawnedObject.GetComponent<Bullet>().SetDirection(facingRight ? Vector3.left : Vector3.right);

        //spawn the bullet as a networked object
        spawnedObject.GetComponent<NetworkObject>().Spawn(true);
    }

    [ServerRpc]
    private void SubmitNameServerRpc(string name, ServerRpcParams rpcParams = default)
    {
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
        {
            // ignore if somehow not dead
            return;
        }

        // server chooses spawn position
        Vector3 spawnPos = Vector3.zero;
        if (SpawnManager.Instance != null)
        {
            spawnPos = SpawnManager.Instance.GetSpawnPosition();
        }

        health.Value = maxHealth;
        isDead.Value = false;

        // tell the owner client to move itself
        RespawnClientRpc(spawnPos, new ClientRpcParams {Send = new ClientRpcSendParams {TargetClientIds = new[] { OwnerClientId }}});
    }

    [ClientRpc]
    private void RespawnClientRpc(Vector3 spawnPos, ClientRpcParams clientRpcParams = default)
    {
        // move player to respawn position
        transform.position = spawnPos;

        // reset player sprite color to fully visible
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
    private void PickupShieldServerRpc(ServerRpcParams rpcParams = default)
    {
        //this runs on the server for this player
        hasShield = true;
        ShieldSpriteClientRpc(true);   // show shield on everyone

        //make sure only one timer is running
        if (shieldCoroutine != null)
        {
            StopCoroutine(shieldCoroutine);
        }
        shieldCoroutine = StartCoroutine(ShieldTimerServer());
    }

    private IEnumerator ShieldTimerServer()
    {
        yield return new WaitForSeconds(10);

        hasShield = false;
        
        //hide shield on everyone
        ShieldSpriteClientRpc(false);  
    }

    [ClientRpc]
    private void ShieldSpriteClientRpc(bool isActive)
    {
        Shield.SetActive(isActive);
    }

    [ServerRpc]
    private void SetInvisibleServerRpc(bool invisible)
    {
        SetInvisibleClientRpc(invisible);
    }

    [ClientRpc]
    private void SetInvisibleClientRpc(bool invisible)
    {
        // exit if no sprite renderer object
        if (playerSpriteGO == null) return;

        // get renderer and exit if missing
        Renderer rend = playerSpriteGO.GetComponent<Renderer>();
        if (rend == null)
        {
            return;
        }

        // 25% visible when invisible, fully visible otherwise
        Color c = rend.material.color;
        if (invisible)
        { 
            c.a = 0.25f;
        }
        else 
        { 
            c.a = 1f;
        }
        
        rend.material.color = c;
    }
}