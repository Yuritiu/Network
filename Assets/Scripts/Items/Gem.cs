using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Gem : NetworkBehaviour
{

    void OnCollisionEnter(Collision target)
    {
        if (target.gameObject.tag.Equals("Player") == true)
        {
            Destroy(gameObject);
        }
    }
}
