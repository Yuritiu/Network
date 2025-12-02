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
        moveDirection = dir.normalized;
    }

    private void Update()
    {
        if (!IsServer)
            return;

        // lifetime
        lifeTimer += Time.deltaTime;
        if (lifeTimer >= maxLifetime)
        {
            ServerDespawn();
            return;
        }

        // move in a straight line
        if (moveDirection != Vector3.zero)
        {
            transform.position += moveDirection * projectileSpeed * Time.deltaTime;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
        if (hasHit) return;

        hasHit = true;

        // Did we hit a player?
        var player = other.GetComponentInParent<playerMovement>();
        if (player != null && player.OwnerClientId != OwnerClientId)
        {
            // just damage, NO knockback
            player.TakeDamage(1);
        }

        // Whether it's a player or wall/anything, despawn the bullet
        ServerDespawn();
    }

    private void ServerDespawn()
    {
        if (IsServer && TryGetComponent<NetworkObject>(out var netObj))
        {
            netObj.Despawn();
        }

        Destroy(gameObject);
    }
}
