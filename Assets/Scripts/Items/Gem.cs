using UnityEngine;

public class Gem : MonoBehaviour
{
    [SerializeField] private float lifeTime = 10f;

    private void Start()
    {
        // Auto-destroy after lifeTime seconds
        Destroy(gameObject, lifeTime);
    }

    private void OnCollisionEnter(Collision target)
    {
        if (target.gameObject.CompareTag("Player"))
        {
            // Destroy immediately on pickup
            Destroy(gameObject);
        }
    }
}