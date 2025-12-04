using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Gem : NetworkBehaviour
{
    [SerializeField] private float despawnTimer;

    public void Awake()
    {
        despawnTimer = 10f;
    }

    private void Update()
    {
        despawnTimer -= Time.deltaTime;

        if(despawnTimer <= 0)
        {
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter(Collision target)
    {
        despawnTimer -= Time.deltaTime;

        if (target.gameObject.tag.Equals("Player") == true)
        {
            Destroy(gameObject);
        }
    }
}
