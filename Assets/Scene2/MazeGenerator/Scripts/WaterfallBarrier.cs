using UnityEngine;
using System.Collections;
using StarterAssets;

public class WaterfallBarrier : MonoBehaviour
{
    [Header("Barrier Settings")]
    [SerializeField] private float knockback_force = 25f; // Increased force
    [SerializeField] private float bounce_height = 5f; // Upward force component
    [SerializeField] private float bounce_cooldown = 1.0f; // Increased cooldown to prevent rapid re-entry

    [Header("Visual Effects")]
    [SerializeField] private GameObject waterfall_effect; // Particle system for waterfall
    [SerializeField] private AudioSource audio_source;
    [SerializeField] private AudioClip bounce_sound;
    [SerializeField] private AudioClip rush_through_sound;

    [Header("Detection")]
    [SerializeField] private float barrier_thickness = 1f; // How thick the barrier is
    [SerializeField] private LayerMask player_layer = -1;

    private BoxCollider barrier_collider;
    private float last_bounce_time;
    private bool player_movement_disabled = false; // Track if we've disabled movement

    void Start()
    {
        Debug.Log($"Waterfall {gameObject.name} starting up!");
        SetupCollider();
        SetupAudio();

        // Ensure waterfall effect is always active
        if (waterfall_effect != null)
        {
            waterfall_effect.SetActive(true);
        }

        Debug.Log($"Waterfall {gameObject.name} setup complete. Collider: {barrier_collider != null}, IsTrigger: {barrier_collider?.isTrigger}");
    }

    private void SetupCollider()
    {
        Debug.Log($"Setting up collider for {gameObject.name}");

        // Create a trigger collider for the waterfall barrier
        barrier_collider = GetComponent<BoxCollider>();
        if (barrier_collider == null)
        {
            Debug.Log("No existing BoxCollider found, creating new one");
            barrier_collider = gameObject.AddComponent<BoxCollider>();
        }
        else
        {
            Debug.Log("Found existing BoxCollider");
        }

        barrier_collider.isTrigger = true;
        barrier_collider.size = new Vector3(barrier_thickness, 3f, 1.2f); // Reduced from 2f depth

        Debug.Log($"Collider setup: Size={barrier_collider.size}, IsTrigger={barrier_collider.isTrigger}");
    }

    private void SetupAudio()
    {
        if (audio_source == null)
        {
            audio_source = GetComponent<AudioSource>();
            if (audio_source == null)
            {
                audio_source = gameObject.AddComponent<AudioSource>();
            }
        }

        // Set up looping waterfall sound
        audio_source.loop = true;
        audio_source.volume = 0.3f;
        if (audio_source.clip == null)
        {
            // You can assign a looping waterfall ambient sound here
        }
        audio_source.Play();
    }

    void OnTriggerEnter(Collider other)
    {
        // Removed excessive debug logging
        HandlePlayerInteraction(other.gameObject);
    }

    // Add this for even more basic debugging
    void OnTriggerStay(Collider other)
    {
        // Removed debug spam
    }

    void OnTriggerExit(Collider other)
    {
        // Removed debug spam
    }

    // Alternative detection method for CharacterController
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"Waterfall: Collision with {collision.gameObject.name}");
        HandlePlayerInteraction(collision.gameObject);
    }

    // Backup method - check for nearby players every frame
    void Update()
    {
        // Debug: Show this script is running (reduced frequency)
        if (Time.frameCount % 1800 == 0) // Every 30 seconds at 60fps
        {
            Debug.Log($"Waterfall {gameObject.name} is active and checking for players...");
        }

        // Check for nearby players every few frames (reduced frequency)
        if (Time.frameCount % 30 == 0) // Every 30 frames since triggers are working
        {
            Collider[] nearbyColliders = Physics.OverlapBox(transform.position,
                new Vector3(barrier_thickness * 0.6f, 2f, 1f), transform.rotation);

            foreach (Collider col in nearbyColliders)
            {
                if (IsPlayerByComponents(col.gameObject))
                {
                    // Only process if not in cooldown to reduce spam
                    if (Time.time - last_bounce_time >= bounce_cooldown)
                    {
                        HandlePlayerInteraction(col.gameObject);
                    }
                    break; // Only handle one player at a time
                }
            }
        }
    }

    private void HandlePlayerInteraction(GameObject playerObject)
    {
        if (!IsPlayerByComponents(playerObject)) return;

        // Try to get Unity's ThirdPersonController
        ThirdPersonController thirdPersonController = playerObject.GetComponent<ThirdPersonController>();
        if (thirdPersonController == null) return;

        // Check if enough time has passed since last bounce
        if (Time.time - last_bounce_time < bounce_cooldown) return;

        bool isSprinting = IsPlayerSprinting(thirdPersonController);

        if (isSprinting)
        {
            // Player is sprinting - let them through
            HandleSprintThrough();
        }
        else
        {
            // Player is not sprinting - bounce them back
            BouncePlayerBack(playerObject);
        }
    }

    private bool IsPlayerSprinting(ThirdPersonController controller)
    {
        // Unity's ThirdPersonController has a sprint property
        StarterAssetsInputs input = controller.GetComponent<StarterAssetsInputs>();

        if (input == null) return false;

        return input.sprint;
    }

    private void HandleSprintThrough()
    {
        Debug.Log("Player sprinted through waterfall!");

        // Play success sound
        if (audio_source != null && rush_through_sound != null)
        {
            audio_source.PlayOneShot(rush_through_sound);
        }

        // Optional: Add some visual effect for successful passage
        StartCoroutine(ShowSuccessEffect());
    }

    private void BouncePlayerBack(GameObject playerObject)
    {
        last_bounce_time = Time.time;

        // Don't bounce if we've already disabled movement recently
        if (player_movement_disabled) return;

        // Calculate bounce direction (away from waterfall)
        Vector3 bounce_direction = (playerObject.transform.position - transform.position).normalized;
        bounce_direction.y = 0; // Keep it purely horizontal
        bounce_direction = bounce_direction.normalized;

        // Larger pushback distance to get player well clear of the trigger
        float pushback_distance = 3f; // Increased from 2f
        Vector3 new_position = playerObject.transform.position + (bounce_direction * pushback_distance);

        // Make sure the new position is on the ground (maintain current Y)
        new_position.y = playerObject.transform.position.y;

        playerObject.transform.position = new_position;

        // Temporarily disable player movement to prevent immediate re-entry (only once)
        ThirdPersonController controller = playerObject.GetComponent<ThirdPersonController>();
        if (controller != null && !player_movement_disabled)
        {
            StartCoroutine(DisableMovementBriefly(controller));
        }

        // Play bounce sound
        if (audio_source != null && bounce_sound != null)
        {
            audio_source.PlayOneShot(bounce_sound);
        }

        // Visual feedback
        StartCoroutine(ShowBounceEffect());

        Debug.Log($"Player teleported back to: {new_position}");
    }

    private IEnumerator DisableMovementBriefly(ThirdPersonController controller)
    {
        // Prevent multiple waterfalls from disabling movement simultaneously
        if (player_movement_disabled) yield break;

        player_movement_disabled = true;

        // Disable movement for a brief moment
        bool originalEnabled = controller.enabled;
        controller.enabled = false;

        yield return new WaitForSeconds(0.3f);

        // Re-enable movement
        controller.enabled = originalEnabled;
        player_movement_disabled = false;
    }

    private IEnumerator ShowSuccessEffect()
    {
        // Optional: Change waterfall color briefly or add sparkle effect
        yield return new WaitForSeconds(0.2f);
        // Reset effect
    }

    private IEnumerator ShowBounceEffect()
    {
        // Optional: Make waterfall flash or add splash effect
        if (waterfall_effect != null)
        {
            // You could change the material color briefly or add extra particles
        }

        yield return new WaitForSeconds(0.3f);
        // Reset effect
    }

    private bool IsPlayerByComponents(GameObject obj)
    {
        // More comprehensive player detection (removed debug spam)
        bool hasController = obj.GetComponent<ThirdPersonController>() != null;
        bool hasCharacterController = obj.GetComponent<CharacterController>() != null;
        bool nameMatch = obj.name.ToLower().Contains("player");

        return hasController || hasCharacterController || nameMatch;
    }

    private bool IsPlayer(Collider other)
    {
        return IsPlayerByComponents(other.gameObject);
    }

    // Visual debugging
    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(barrier_thickness, 3f, 1f));

        // Draw direction arrow
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * 2f);
    }
}