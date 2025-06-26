using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class EndRoomTrigger : MonoBehaviour
{
    [Header("Cutscene References")]
    public GameObject coin;
    public Transform player;
    public Camera playerCamera;

    [Header("Animation Settings")]
    public float coinRiseHeight = 10f;
    public float animationDuration = 3f;
    public float playerMoveSpeed = 2f;
    public string nextSceneName = "NextLevel";

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip coinRiseSound;

    private bool triggered = false;
    private Vector3 coinStartPos;
    private Vector3 playerStartPos;
    private Quaternion cameraStartRot;

    // Store player components to disable/enable
    private MonoBehaviour[] playerMovementScripts;
    private CharacterController characterController;
    private Rigidbody playerRigidbody;

    void Start()
    {
        // The coin reference is already set by MazeSpawner, so we don't need to find it by tag
        // Auto-find player reference if not set
        if (player == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }
        if (playerCamera == null)
            playerCamera = Camera.main;

        // Find player movement components
        if (player != null)
        {
            // Get all MonoBehaviour scripts on player (likely includes movement scripts)
            playerMovementScripts = player.GetComponents<MonoBehaviour>();
            characterController = player.GetComponent<CharacterController>();
            playerRigidbody = player.GetComponent<Rigidbody>();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!triggered && other.CompareTag("Player"))
        {
            triggered = true;
            StartCutscene();
        }
    }

    void StartCutscene()
    {
        Debug.Log("End room cutscene triggered!");

        // Store starting positions
        if (coin != null)
            coinStartPos = coin.transform.position;
        if (player != null)
            playerStartPos = player.position;
        if (playerCamera != null)
            cameraStartRot = playerCamera.transform.rotation;

        // Disable player movement
        DisablePlayerControls();

        // Play sound
        if (audioSource != null && coinRiseSound != null)
            audioSource.PlayOneShot(coinRiseSound);

        // Start animation
        StartCoroutine(CutsceneAnimation());
    }

    void DisablePlayerControls()
    {
        // Disable player movement scripts (you may need to customize this based on your player controller)
        if (playerMovementScripts != null)
        {
            foreach (var script in playerMovementScripts)
            {
                // Skip this trigger script and essential Unity components
                if (script != this &&
                    !(script is Transform) &&
                    !(script is Camera) &&
                    !(script is AudioSource) &&
                    script.GetType().Name.ToLower().Contains("move") ||
                    script.GetType().Name.ToLower().Contains("control") ||
                    script.GetType().Name.ToLower().Contains("player"))
                {
                    script.enabled = false;
                    Debug.Log($"Disabled player script: {script.GetType().Name}");
                }
            }
        }

        // Disable character controller
        if (characterController != null)
            characterController.enabled = false;

        // Stop rigidbody movement
        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
            playerRigidbody.isKinematic = true;
        }
    }

    void EnablePlayerControls()
    {
        // Re-enable player movement scripts
        if (playerMovementScripts != null)
        {
            foreach (var script in playerMovementScripts)
            {
                if (script != this &&
                    !(script is Transform) &&
                    !(script is Camera) &&
                    !(script is AudioSource))
                {
                    script.enabled = true;
                }
            }
        }

        // Re-enable character controller
        if (characterController != null)
            characterController.enabled = true;

        // Re-enable rigidbody
        if (playerRigidbody != null)
            playerRigidbody.isKinematic = false;
    }

    IEnumerator CutsceneAnimation()
    {
        float elapsedTime = 0f;
        Vector3 coinTargetPos = coinStartPos + Vector3.up * coinRiseHeight;
        Vector3 playerTargetPos = transform.position; // Move player to trigger center
        playerTargetPos.y = playerStartPos.y; // Keep player at ground level initially

        // Calculate player's final position (rising up following the coin)
        Vector3 playerFinalPos = playerTargetPos + Vector3.up * (coinRiseHeight * 0.8f); // Player rises a bit less than coin

        // Calculate player rotations
        Quaternion playerStartRotation = player.rotation;
        Quaternion playerVerticalRotation = Quaternion.Euler(playerStartRotation.eulerAngles.x - 90f, playerStartRotation.eulerAngles.y, playerStartRotation.eulerAngles.z); // Rotate -90 degrees so head points up

        // Calculate when to cut scene (80% of the way to ceiling)
        float cutoffHeight = coinStartPos.y + (coinRiseHeight * 0.8f);
        bool shouldCutScene = false;

        while (elapsedTime < animationDuration && !shouldCutScene)
        {
            float progress = elapsedTime / animationDuration;
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            // Animate coin rising
            if (coin != null)
            {
                coin.transform.position = Vector3.Lerp(coinStartPos, coinTargetPos, smoothProgress);

                // Check if coin has reached cutoff height (80% of target)
                if (coin.transform.position.y >= cutoffHeight)
                {
                    shouldCutScene = true;
                }

                // Add spinning effect to coin
                coin.transform.Rotate(0, 180 * Time.deltaTime, 0);

                // Optional: Add floating/bobbing effect
                float bob = Mathf.Sin(Time.time * 3f) * 0.1f;
                coin.transform.position += Vector3.up * bob * smoothProgress;
            }

            // Animate player
            if (player != null)
            {
                // Phase 1 (first 30% of animation): Move to center and start rotating
                if (progress < 0.3f)
                {
                    float phaseProgress = progress / 0.3f;

                    // Move toward center
                    player.position = Vector3.Lerp(playerStartPos, playerTargetPos, phaseProgress);

                    // Start rotating to stand upright (perpendicular to floor)
                    player.rotation = Quaternion.Slerp(playerStartRotation, playerVerticalRotation, phaseProgress);
                }
                // Phase 2 (30% to 100%): Continue rotating and start rising
                else
                {
                    float phaseProgress = (progress - 0.3f) / 0.7f;

                    // Stay at center horizontally, but start rising
                    Vector3 currentPos = Vector3.Lerp(playerTargetPos, playerFinalPos, phaseProgress);
                    player.position = currentPos;

                    // Complete the vertical rotation (standing upright)
                    player.rotation = Quaternion.Slerp(playerStartRotation, playerVerticalRotation, Mathf.Min(1f, progress / 0.6f));
                }
            }

            // Gradually tilt camera up to follow coin
            if (playerCamera != null)
            {
                if (coin != null)
                {
                    // Calculate the angle to look at the coin
                    Vector3 directionToCoin = (coin.transform.position - playerCamera.transform.position).normalized;
                    float lookUpAngle = Mathf.Asin(directionToCoin.y) * Mathf.Rad2Deg;

                    // Gradually increase the look-up angle
                    float targetAngle = Mathf.Lerp(0, lookUpAngle, smoothProgress);

                    // Apply the rotation to the camera
                    Quaternion targetRotation = cameraStartRot * Quaternion.Euler(targetAngle, 0, 0);
                    playerCamera.transform.rotation = Quaternion.Lerp(cameraStartRot, targetRotation, smoothProgress);
                }
                else
                {
                    // Fallback: just tilt up gradually
                    float lookUpAngle = 45f * smoothProgress;
                    Quaternion targetRot = cameraStartRot * Quaternion.Euler(lookUpAngle, 0, 0);
                    playerCamera.transform.rotation = Quaternion.Lerp(cameraStartRot, targetRot, smoothProgress);
                }
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Cut to next scene immediately (no wait)
        TransitionToNextScene();
    }

    void TransitionToNextScene()
    {
        Debug.Log($"Transitioning to scene: {nextSceneName}");

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            // Check if scene exists in build settings
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);

                if (sceneName == nextSceneName)
                {
                    SceneManager.LoadScene(nextSceneName);
                    return;
                }
            }

            // Scene not found in build settings
            Debug.LogError($"Scene '{nextSceneName}' not found in build settings! Please add it to File > Build Settings > Scenes in Build");
        }
        else
        {
            Debug.LogWarning("Next scene name not set! Cutscene completed but no scene to load.");
        }
    }

    // For debugging in editor
    void OnDrawGizmos()
    {
        // Draw trigger area
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, GetComponent<SphereCollider>()?.radius ?? 1f);

        // Draw coin rise path
        if (coin != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(coin.transform.position, coin.transform.position + Vector3.up * coinRiseHeight);
            Gizmos.DrawWireSphere(coin.transform.position + Vector3.up * coinRiseHeight, 0.5f);
        }
    }
}