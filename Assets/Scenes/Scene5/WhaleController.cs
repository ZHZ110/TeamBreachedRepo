using UnityEngine;

public class WhaleController : MonoBehaviour
{
    [Header("Movement")]
    public float moveDistance = 2f; // How far the whale moves per input
    public float moveSpeed = 5f; // Speed of movement animation

    [Header("Input")]
    public KeyCode moveKey = KeyCode.Space; // Key to move the whale

    [Header("Beach Escape")]
    public bool mustMoveTowardSea = true; // Force whale to only move toward deeper water

    private BeachWaveController beachWaves;
    private Vector3 targetPosition;
    private bool isMoving = false;

    void Start()
    {
        beachWaves = FindObjectOfType<BeachWaveController>();
        targetPosition = transform.position;

        if (beachWaves == null)
        {
            Debug.LogWarning("No BeachWaveController found! Whale movement won't be affected by waves.");
        }
    }

    void Update()
    {
        HandleInput();
        MoveToTarget();
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(moveKey) && !isMoving)
        {
            AttemptMove();
        }
    }

    void AttemptMove()
    {
        if (beachWaves == null) return;

        // Get the current wave state
        float waveMultiplier = beachWaves.GetBeachWaveMultiplier(transform.position);
        bool isAdvancing = beachWaves.IsWaveAdvancing();

        // Calculate move distance based on wave state
        float actualMoveDistance = moveDistance * waveMultiplier;

        // Determine movement direction
        Vector3 moveDirection;
        if (mustMoveTowardSea)
        {
            // Always move toward deeper water (whale trying to escape beach)
            moveDirection = (beachWaves.GetDeepWaterPosition() - transform.position).normalized;
        }
        else
        {
            // Generic movement (you can change this to your preferred direction)
            moveDirection = transform.forward;
        }

        Vector3 newPosition = transform.position + moveDirection * actualMoveDistance;

        // Set target and start moving
        targetPosition = newPosition;
        isMoving = true;

        // Debug output
        string waveState = isAdvancing ? "ADVANCING (hard to escape)" : "RETREATING (easy escape!)";
        Debug.Log($"Wave is {waveState} - Moving {actualMoveDistance:F1} units toward sea");
    }

    void MoveToTarget()
    {
        if (isMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                isMoving = false;
            }
        }
    }

    // Visual indicator of whale's struggle
    void OnDrawGizmos()
    {
        if (beachWaves != null && Application.isPlaying)
        {
            float multiplier = beachWaves.GetBeachWaveMultiplier(transform.position);
            bool advancing = beachWaves.IsWaveAdvancing();

            // Draw movement potential
            Gizmos.color = advancing ? Color.red : Color.green;
            Vector3 moveDir = mustMoveTowardSea ?
                (beachWaves.GetDeepWaterPosition() - transform.position).normalized :
                transform.forward;

            float visualDistance = moveDistance * multiplier;
            Gizmos.DrawLine(transform.position, transform.position + moveDir * visualDistance);

            // Draw whale state
#if UNITY_EDITOR
            string state = advancing ? "STRUGGLING AGAINST WAVES" : "RIDING RETREATING WAVE";
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, state);
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1f, $"Move Distance: {visualDistance:F1}");
#endif
        }
    }
}