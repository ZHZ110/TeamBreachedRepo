// PlayerRockPusherInputSystem.cs - Alternative version for Unity's new Input System
// Only use this if you're using Unity's Input System package instead of the legacy Input Manager

using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerRockPusherInputSystem : MonoBehaviour
{
    [Header("Push Settings")]
    public float pushRange = 1.5f;
    public LayerMask rockLayerMask = 1 << 8;

    [Header("Input Actions")]
    public InputActionReference pushAction;

    private PushableRock currentRock;
    private bool isPushing = false;

    void OnEnable()
    {
        pushAction.action.performed += OnPushPerformed;
        pushAction.action.canceled += OnPushCanceled;
    }

    void OnDisable()
    {
        pushAction.action.performed -= OnPushPerformed;
        pushAction.action.canceled -= OnPushCanceled;
    }

    void OnPushPerformed(InputAction.CallbackContext context)
    {
        TryStartPushing();
    }

    void OnPushCanceled(InputAction.CallbackContext context)
    {
        StopPushing();
    }

    void Update()
    {
        // Check if still in range while pushing
        if (isPushing && currentRock != null)
        {
            float distance = Vector3.Distance(transform.position, currentRock.transform.position);
            if (distance > pushRange)
            {
                StopPushing();
            }
        }
    }

    void TryStartPushing()
    {
        // Find rocks in push range
        Collider[] nearbyRocks = Physics.OverlapSphere(transform.position, pushRange, rockLayerMask);

        PushableRock closestRock = null;
        float closestDistance = float.MaxValue;

        foreach (Collider rockCollider in nearbyRocks)
        {
            PushableRock rock = rockCollider.GetComponent<PushableRock>();
            if (rock != null)
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
            Vector3 directionToRock = (closestRock.transform.position - transform.position).normalized;
            Vector3 pushDirection = GetCardinalDirection(directionToRock);

            currentRock = closestRock;
            currentRock.StartPushing(pushDirection, transform);
            isPushing = true;
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
    }

    Vector3 GetCardinalDirection(Vector3 direction)
    {
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
}