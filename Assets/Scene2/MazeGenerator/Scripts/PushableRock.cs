using UnityEngine;

public class PushableRock : MonoBehaviour
{
    [Header("Rock Settings")]
    public float pushSpeed = 3f; // Increased speed for testing
    public float cellSize = 5f; // Make this adjustable in inspector
    public LayerMask wallLayerMask = 1; // Set this to your wall layer
    public float raycastDistance = 0.6f; // Slightly more than half cell width

    private bool isBeingPushed = false;
    private Vector3 pushDirection;
    private Rigidbody rb;
    private Vector3 targetPosition;
    private Vector3 startPosition;
    private bool isMoving = false;
    private Transform authorizedPusher; // Only this transform can push the rock

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        // Make the rock completely immovable by physics
        rb.isKinematic = true; // This prevents ALL physics-based movement
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        Debug.Log($"Rock {gameObject.name} initialized as kinematic at position {transform.position}");
    }

    void Update()
    {
        if (isBeingPushed && isMoving && authorizedPusher != null)
        {
            // Check if there's a wall in the push direction
            Vector3 rayStart = transform.position + Vector3.up * 0.1f;

            if (Physics.Raycast(rayStart, pushDirection, raycastDistance, wallLayerMask))
            {
                // Hit a wall, stop pushing
                Debug.Log("Rock hit wall, stopping");
                StopPushing();
                return;
            }

            // Show current movement progress
            float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
            float totalDistance = Vector3.Distance(startPosition, targetPosition);
            float progress = 1f - (distanceToTarget / totalDistance);

            // Move towards target position using transform (since we're kinematic)
            Vector3 newPosition = Vector3.MoveTowards(transform.position, targetPosition, pushSpeed * Time.deltaTime);
            transform.position = newPosition;

            Debug.Log($"Rock moving: {progress:P1} complete, distance remaining: {distanceToTarget:F2}");

            // Check if we've reached the target
            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                // Snap to grid position and stop
                transform.position = targetPosition;
                isMoving = false;
                Debug.Log($"Rock reached target position: {targetPosition}");
            }
        }
    }

    public void StartPushing(Vector3 direction, Transform pusher)
    {
        // Only allow pushing if not already moving
        if (isMoving)
        {
            Debug.Log("Rock is already moving, cannot push");
            return;
        }

        // Validate that the pusher is behind the rock relative to push direction
        Vector3 toPusher = (pusher.position - transform.position).normalized;
        float alignment = Vector3.Dot(toPusher, -direction);

        Debug.Log($"Push attempt: direction={direction}, alignment={alignment}, pusher pos={pusher.position}, rock pos={transform.position}");

        if (alignment > 0.2f) // More lenient alignment check
        {
            pushDirection = direction;
            isBeingPushed = true;
            authorizedPusher = pusher;
            startPosition = transform.position;

            // Calculate target position (one grid cell away)
            targetPosition = transform.position + direction * cellSize;
            isMoving = true;

            Debug.Log($"Rock authorized to move from {startPosition} to {targetPosition} by {pusher.name}");
            Debug.Log($"Movement distance: {Vector3.Distance(startPosition, targetPosition)} units");
            Debug.Log($"Push speed: {pushSpeed} units/second");
            Debug.Log($"Estimated time: {Vector3.Distance(startPosition, targetPosition) / pushSpeed} seconds");
        }
        else
        {
            Debug.Log($"Push denied - bad alignment: {alignment} (need > 0.2)");
        }
    }

    public void StopPushing()
    {
        isBeingPushed = false;
        isMoving = false;
        authorizedPusher = null;
        Debug.Log($"Rock pushing stopped at position: {transform.position}");
    }

    public bool IsMoving()
    {
        return isMoving;
    }

    void OnDrawGizmos()
    {
        // Always show rock position
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);

        if (isBeingPushed)
        {
            Gizmos.color = Color.red;
            Vector3 rayStart = transform.position + Vector3.up * 0.1f;
            Gizmos.DrawLine(rayStart, rayStart + pushDirection * raycastDistance);

            if (isMoving)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(targetPosition, 0.5f);
                Gizmos.DrawLine(transform.position, targetPosition);

                // Show start position too
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(startPosition, 0.3f);
            }
        }

        // Show if rock is authorized
        if (authorizedPusher != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 2, Vector3.one * 0.5f);
        }
    }

    // Log any collision attempts for debugging
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"Rock collision with {collision.gameObject.name} - Authorized pusher: {(authorizedPusher != null ? authorizedPusher.name : "None")}");
    }
}