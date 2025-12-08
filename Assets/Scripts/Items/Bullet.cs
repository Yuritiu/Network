using System;
using Unity.Netcode;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    [Header("Movement")]
    public float projectileSpeed = 5f;
    private Vector3 moveDirection;

    [Header("Lifetime")]
    public float maxLifetime = 3f; // time before it deletes
    private float lifeTimer = 0f;
    private bool hasHit = false;

    public ulong OwnerClientId;

    public void SetDirection(Vector3 dir)
    {
        // set normalized travel direction
        moveDirection = dir.normalized;
    }

    private void Update()
    {
        // server only controls bullet movement
        if (!IsServer)
        {
            return;
        }

        // track lifetime
        lifeTimer += Time.deltaTime;
        if (lifeTimer >= maxLifetime)
        {
            ServerDespawn();
            return;
        }

        // move bullet forward
        if (moveDirection != Vector3.zero)
        {
            transform.position += moveDirection * projectileSpeed * Time.deltaTime;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // server handles collisions
        if (!IsServer)
        {
            return;
        }
        // ignore multiple hits
        if (hasHit)
        {
            return;
        }

        hasHit = true;

        // check if a player was hit
        playerManager player = other.GetComponentInParent<playerManager>();

        // apply damage if not self hit
        if (player != null && player.OwnerClientId != OwnerClientId)
        {
            player.TakeDamage(1, OwnerClientId);
        }

        // despawn bullet after hit
        ServerDespawn();
    }

    private void ServerDespawn()
    {
        // despawn network object on server
        if (IsServer && TryGetComponent<NetworkObject>(out var netObj))
        {
            netObj.Despawn();
        }

        Destroy(gameObject);
    }
}
