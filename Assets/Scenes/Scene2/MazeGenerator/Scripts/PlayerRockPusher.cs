using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerRockPusher : MonoBehaviour
{
    [Header("Push Settings")]
    public float pushRange = 5f; // Increased from 1.5f to 5f
    public LayerMask rockLayerMask = 1 << 8;

    [Header("Input Actions")]
    [Tooltip("Use your existing jump action here")]
    public InputActionReference pushAction;

    [Header("Audio")]
    public AudioClip pushSound;
    public AudioSource pushAudioSource;
    [Range(0f, 1f)]
    public float pushSoundVolume = 1f;

    private PushableRock currentRock;
    private bool isPushing = false;
    private bool buttonPressed = false;

    void Start()
    {
        // Create dedicated AudioSource for push sounds to avoid conflicts
        if (pushAudioSource == null)
        {
            GameObject pushAudioGO = new GameObject("PushAudio");
            pushAudioGO.transform.SetParent(transform);
            pushAudioGO.transform.localPosition = Vector3.zero;
            pushAudioSource = pushAudioGO.AddComponent<AudioSource>();
        }

        // Setup the dedicated AudioSource
        pushAudioSource.playOnAwake = false;
        pushAudioSource.spatialBlend = 0f;

        //Debug.Log($"Rock pusher audio setup complete. Clip assigned: {pushSound != null}");
    }

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
        //Debug.Log("★ BUTTON PRESSED ★");
        buttonPressed = true;

        // Check if near rock and play sound + start pushing
        if (IsNearRock())
        {
            // Play sound immediately
            if (pushSound != null && pushAudioSource != null)
            {
                pushAudioSource.clip = pushSound;
                pushAudioSource.volume = pushSoundVolume;
                pushAudioSource.Play();
                //Debug.Log("PUSH SOUND PLAYED - NEAR ROCK");
            }

            // Start the pushing logic
            StartPushing();
        }
        else
        {
            //Debug.Log("Space pressed but not near rock - no push sound");
        }
    }

    void OnPushCanceled(InputAction.CallbackContext context)
    {
        //Debug.Log("Button released");
        buttonPressed = false;
        StopPushing();
    }

    bool IsNearRock()
    {
        Collider[] nearbyRocks = Physics.OverlapSphere(transform.position, pushRange, rockLayerMask);
        return nearbyRocks.Length > 0;
    }

    void Update()
    {
        // Only handle continuing pushes, not starting new ones
        if (isPushing && currentRock != null)
        {
            float distance = Vector3.Distance(transform.position, currentRock.transform.position);

            // Stop if too far or button released
            if (distance > pushRange || !buttonPressed)
            {
                StopPushing();
                return;
            }

            // Continue pushing when rock stops moving
            if (!currentRock.IsMoving() && buttonPressed)
            {
                //Debug.Log("Rock stopped, continuing push");
                ContinuePushing();
            }
        }
    }

    void StartPushing()
    {
        if (!buttonPressed)
        {
            //Debug.Log("StartPushing: button not pressed, returning");
            return;
        }

        //Debug.Log("StartPushing: Looking for rocks...");
        Collider[] nearbyRocks = Physics.OverlapSphere(transform.position, pushRange, rockLayerMask);
        //Debug.Log($"StartPushing: Found {nearbyRocks.Length} rocks");

        PushableRock closestRock = null;
        float closestDistance = float.MaxValue;

        foreach (Collider rockCollider in nearbyRocks)
        {
            PushableRock rock = rockCollider.GetComponent<PushableRock>();
            if (rock != null && !rock.IsMoving())
            {
                float distance = Vector3.Distance(transform.position, rock.transform.position);
                //Debug.Log($"StartPushing: Found valid rock at distance {distance}");
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestRock = rock;
                }
            }
            else
            {
                //Debug.Log($"StartPushing: Rock invalid - PushableRock component: {rock != null}, IsMoving: {rock?.IsMoving()}");
            }
        }

        if (closestRock != null)
        {
            Vector3 directionToRock = (closestRock.transform.position - transform.position).normalized;
            Vector3 pushDirection = GetCardinalDirection(directionToRock);

            currentRock = closestRock;
            isPushing = true;

            //Debug.Log($"StartPushing: About to call StartPushing on rock with direction: {pushDirection}");
            currentRock.StartPushing(pushDirection, transform);
            //Debug.Log("StartPushing: Rock.StartPushing() called successfully");
        }
        else
        {
            //Debug.Log("StartPushing: No valid rock found!");
        }
    }

    void ContinuePushing()
    {
        if (currentRock == null || !buttonPressed) return;

        // Play sound for continued pushing
        if (pushSound != null && pushAudioSource != null)
        {
            pushAudioSource.clip = pushSound;
            pushAudioSource.volume = pushSoundVolume;
            pushAudioSource.Play();
            //Debug.Log("Continue push sound played");
        }

        // Determine new push direction
        Vector3 directionToRock = (currentRock.transform.position - transform.position).normalized;
        Vector3 pushDirection = GetCardinalDirection(directionToRock);

        // Continue pushing
        currentRock.StartPushing(pushDirection, transform);
    }

    void StopPushing()
    {
        if (currentRock != null)
        {
            currentRock.StopPushing();
            currentRock = null;
        }
        isPushing = false;
        //Debug.Log("Stopped pushing");
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

    public bool IsPushing()
    {
        return isPushing;
    }

    public bool IsButtonPressed()
    {
        return buttonPressed;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = isPushing ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pushRange);

        if (isPushing && currentRock != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentRock.transform.position);
        }

        if (buttonPressed)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 2, Vector3.one * 0.5f);
        }
    }
}