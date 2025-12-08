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
        // disable if missing points
        if (pointA == null || pointB == null)
        {
            enabled = false;
            return;
        }

        // set starting position
        movingToB = startAtA;
        if (startAtA)
        {
            transform.position = pointA.position;
        }
        else
        {
            transform.position = pointB.position;
        }
     
        if (movingToB)
        {
            targetPos = pointB.position;
        }
        else
        {
            targetPos = pointA.position;
        }
    }

    private void Update()
    {
        // wait at ends if enabled
        if (waitAtEnds && waitTimer > 0f)
        {
            waitTimer -= Time.deltaTime;
            return;
        }

        // move toward target
        transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

        // reached target
        if (Vector3.Distance(transform.position, targetPos) < 0.01f)
        {
            movingToB = !movingToB;
            if (movingToB)
            {
                targetPos = pointB.position;
            }
            else
            {
                targetPos = pointA.position;
            }

            if (waitAtEnds)
            {
                waitTimer = waitTime;
            }
        }
    }
}
