using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    [Header("Movement")]
    public float projectileSpeed = 5f;
    private Vector3 moveDirection;
    private Rigidbody rb;

    [Header("Lifetime")]
    public float maxLifetime = 3f; //time before it deletes
    private float lifeTimer = 0f;

    public ulong OwnerClientId;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void SetDirection(Vector3 dir)
    {
        moveDirection = dir.normalized;
    }

    private void Update()
    {
        if (!IsServer)
        {
            return;
        }

        lifeTimer += Time.deltaTime;
        if (lifeTimer >= maxLifetime)
        {
            DespawnBullet();
        }
    }

    private void FixedUpdate()
    {
        if (!IsServer)
        {
            return;
        }

        if (moveDirection != Vector3.zero)
        {
            rb.velocity = moveDirection * projectileSpeed;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!IsServer)
        {
            return;
        }

        //checks if a player has been hit
        playerMovement player = collision.gameObject.GetComponent<playerMovement>();
        
        if (player != null)
        {
            if (player.OwnerClientId != OwnerClientId)
            {
                player.TakeDamage(1);
            }
        }

        DespawnBullet();
    }

    private void DespawnBullet()
    {
        GetComponent<NetworkObject>().Despawn(true);
        Destroy(this);
    }
}
