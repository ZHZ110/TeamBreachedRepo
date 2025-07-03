using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerRockPusher : MonoBehaviour
{
    [Header("Push Settings")]
    public float pushRange = 1.5f; // How close player needs to be to push
    public LayerMask rockLayerMask = 1 << 8; // Set this to your rock layer

    [Header("Input Actions")]
    [Tooltip("Use your existing jump action here")]
    public InputActionReference pushAction; // Connect this to your jump button

    private PushableRock currentRock;
    private bool isPushing = false;
    private bool buttonPressed = false;

    void OnEnable()
    {
        if (pushAction != null)
        {
            pushAction.action.started += OnPushStarted;
            pushAction.action.canceled += OnPushCanceled;
            pushAction.action.Enable();
        }
    }

    void OnDisable()
    {
        if (pushAction != null)
        {
            pushAction.action.started -= OnPushStarted;
            pushAction.action.canceled -= OnPushCanceled;
            pushAction.action.Disable();
        }
    }

    void OnPushStarted(InputAction.CallbackContext context)
    {
        buttonPressed = true;
        //Debug.Log("Push button pressed");

        // Only try to push if we're near a rock
        if (IsNearRock())
        {
            TryStartPushing();
        }
    }

    void OnPushCanceled(InputAction.CallbackContext context)
    {
        buttonPressed = false;
        //Debug.Log("Push button released");
        StopPushing();
    }

    bool IsNearRock()
    {
        // Quick check to see if there's a rock nearby
        Collider[] nearbyRocks = Physics.OverlapSphere(transform.position, pushRange, rockLayerMask);
        return nearbyRocks.Length > 0;
    }

    void Update()
    {
        // Check if still in range while pushing
        if (isPushing && currentRock != null)
        {
            float distance = Vector3.Distance(transform.position, currentRock.transform.position);
            if (distance > pushRange || !buttonPressed)
            {
                StopPushing();
                return; // Exit early after stopping
            }

            // If rock finished moving and button is still pressed, allow pushing again
            if (currentRock != null && !currentRock.IsMoving() && buttonPressed)
            {
                //Debug.Log("Rock finished moving, allowing new push");
                // Reset the pushing state to allow a new push
                isPushing = false;
                currentRock = null;
                // Then try to start pushing again
                TryStartPushing();
            }
        }
        else if (isPushing && currentRock == null)
        {
            // Safety check - if we're marked as pushing but have no rock, stop
            isPushing = false;
            //Debug.Log("Stopped pushing - currentRock was null");
        }

        // Only try to start pushing if we're not already pushing
        else if (buttonPressed && IsNearRock() && !isPushing)
        {
            //Debug.Log("Button held near rock but not pushing - trying to start");
            TryStartPushing();
        }
    }

    void TryStartPushing()
    {
        if (!buttonPressed) return; // Don't push unless button is held

        // Find rocks in push range
        Collider[] nearbyRocks = Physics.OverlapSphere(transform.position, pushRange, rockLayerMask);

        PushableRock closestRock = null;
        float closestDistance = float.MaxValue;

        foreach (Collider rockCollider in nearbyRocks)
        {
            PushableRock rock = rockCollider.GetComponent<PushableRock>();
            if (rock != null && !rock.IsMoving()) // Don't try to push moving rocks
            {
                float distance = Vector3.Distance(transform.position, rock.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestRock = rock;
                }
            }
        }

        if (closestRock != null)
        {
            // Determine push direction based on player position relative to rock
            Vector3 directionToRock = (closestRock.transform.position - transform.position).normalized;
            Vector3 pushDirection = GetCardinalDirection(directionToRock);

            currentRock = closestRock;
            currentRock.StartPushing(pushDirection, transform);
            isPushing = true;

            //Debug.Log($"Started pushing rock in direction: {pushDirection}");
        }
        else
        {
            //Debug.Log("No pushable rock found in range");
        }
    }

    void StopPushing()
    {
        if (currentRock != null)
        {
            currentRock.StopPushing();
            currentRock = null;
        }
        isPushing = false;
        //Debug.Log("Stopped pushing rock");
    }

    Vector3 GetCardinalDirection(Vector3 direction)
    {
        // Convert any direction to the closest cardinal direction (N, S, E, W)
        float x = Mathf.Abs(direction.x);
        float z = Mathf.Abs(direction.z);

        if (x > z)
        {
            return direction.x > 0 ? Vector3.right : Vector3.left;
        }
        else
        {
            return direction.z > 0 ? Vector3.forward : Vector3.back;
        }
    }

    // Public method to check if currently pushing (your movement script can use this)
    public bool IsPushing()
    {
        return isPushing;
    }

    // Public method to check if button is pressed (for jump script)
    public bool IsButtonPressed()
    {
        return buttonPressed;
    }

    void OnDrawGizmos()
    {
        // Show push range
        Gizmos.color = isPushing ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pushRange);

        if (isPushing && currentRock != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentRock.transform.position);
        }

        // Show if button is pressed
        if (buttonPressed)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 2, Vector3.one * 0.5f);
        }
    }
}