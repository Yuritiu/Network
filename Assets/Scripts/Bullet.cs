using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float projectileSpeed = 5f;
    private Vector3 moveDirection;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void SetDirection(Vector3 dir)
    {
        moveDirection = dir.normalized;
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
        GetComponent<NetworkObject>().Despawn(true);
        Destroy(this);
    }
}
