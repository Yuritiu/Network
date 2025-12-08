using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [Header("Path")]
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;

    [Header("Movement")]
    [SerializeField] private float speed = 2f;
    [SerializeField] private bool startAtA = true;
    [SerializeField] private bool waitAtEnds = false;
    [SerializeField] private float waitTime = 1f;

    private Vector3 targetPos;
    private bool movingToB;
    private float waitTimer;

    private void Start()
    {
        if (pointA == null || pointB == null)
        {
            Debug.LogError("[MovingPlatform] pointA and pointB must be assigned.");
            enabled = false;
            return;
        }

        movingToB = startAtA;
        transform.position = startAtA ? pointA.position : pointB.position;
        targetPos = movingToB ? pointB.position : pointA.position;
    }

    private void Update()
    {
        if (waitAtEnds && waitTimer > 0f)
        {
            waitTimer -= Time.deltaTime;
            return;
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPos,
            speed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, targetPos) < 0.01f)
        {
            movingToB = !movingToB;
            targetPos = movingToB ? pointB.position : pointA.position;

            if (waitAtEnds)
            {
                waitTimer = waitTime;
            }
        }
    }
}
