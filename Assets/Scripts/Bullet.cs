using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Movement")]
    public float projectileSpeed = 5f;
    private Vector3 moveDirection;

    [Header("Lifetime")]
    public float maxLifetime = 3f; //time before it deletes
    private float lifeTimer = 0f;

    private Rigidbody rb;

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
        lifeTimer += Time.deltaTime;
        if (lifeTimer >= maxLifetime)
        {
            DespawnBullet();
        }
    }

    private void FixedUpdate()
    {
        if (moveDirection != Vector3.zero)
        {
            rb.velocity = moveDirection * projectileSpeed;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        DespawnBullet();
    }

    private void DespawnBullet()
    {
        GetComponent<NetworkObject>().Despawn(true);
        Destroy(this);
    }
}
