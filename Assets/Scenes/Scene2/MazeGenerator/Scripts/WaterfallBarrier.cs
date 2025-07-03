using UnityEngine;
using System.Collections;
using StarterAssets;

public class WaterfallBarrier : MonoBehaviour
{
    [Header("Barrier Settings")]
    [SerializeField] private float bounce_cooldown = 1.0f;

    [Header("Visual Effects")]
    [SerializeField] private GameObject waterfall_effect;
    [SerializeField] private AudioSource audio_source;
    [SerializeField] private AudioClip bounce_sound;
    [SerializeField] private AudioClip rush_through_sound;

    [Header("Detection")]
    [SerializeField] private float barrier_thickness = 1f;
    [SerializeField] private LayerMask player_layer = -1;

    private BoxCollider barrier_collider;
    private float last_bounce_time;
    private bool player_movement_disabled = false;

    void Start()
    {
        //Debug.Log($"Waterfall {gameObject.name} starting up!");
        SetupCollider();
        SetupAudio();

        if (waterfall_effect != null)
        {
            waterfall_effect.SetActive(true);
        }

        //Debug.Log($"Waterfall {gameObject.name} setup complete. Collider: {barrier_collider != null}, IsTrigger: {barrier_collider?.isTrigger}");

        // Debug: Find and log player info
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            //Debug.Log($"Found player: {player.name}");
            //Debug.Log($"Player has ThirdPersonController: {player.GetComponent<ThirdPersonController>() != null}");
            //Debug.Log($"Player has CharacterController: {player.GetComponent<CharacterController>() != null}");
        }
        else
        {
            //Debug.LogWarning("No player found with 'Player' tag!");
        }
    }

    private void SetupCollider()
    {
        //Debug.Log($"Setting up collider for {gameObject.name}");

        // First, let's see what colliders exist on this object and its children
        Collider[] allColliders = GetComponentsInChildren<Collider>();
        //Debug.Log($"Found {allColliders.Length} total colliders in {gameObject.name} and its children:");

        for (int i = 0; i < allColliders.Length; i++)
        {
            //Debug.Log($"  Collider {i}: {allColliders[i].GetType().Name} on '{allColliders[i].gameObject.name}', IsTrigger: {allColliders[i].isTrigger}");
        }

        // Try to get BoxCollider from root object first
        barrier_collider = GetComponent<BoxCollider>();

        if (barrier_collider == null)
        {
            // Try to find a BoxCollider in children
            barrier_collider = GetComponentInChildren<BoxCollider>();
            if (barrier_collider != null)
            {
                //Debug.Log($"Found existing BoxCollider on child object: {barrier_collider.gameObject.name}");
            }
        }
        else
        {
            //Debug.Log("Found existing BoxCollider on root object");
        }

        if (barrier_collider == null)
        {
            //Debug.Log("No existing BoxCollider found anywhere, creating new one on root");
            barrier_collider = gameObject.AddComponent<BoxCollider>();

            // Set up the new collider with appropriate size
            barrier_collider.size = new Vector3(
                barrier_thickness * 2f,  // Make it wider
                4f,                      // Taller to catch jumping players
                2f                       // Deeper to ensure detection
            );
            barrier_collider.center = Vector3.zero;
        }
        else
        {
            //Debug.Log($"Using existing BoxCollider. Current size: {barrier_collider.size}, Current center: {barrier_collider.center}");
            //Debug.Log($"Parent scale: {transform.localScale}, Collider parent scale: {barrier_collider.transform.lossyScale}");

            // Calculate the actual world size of the collider
            Vector3 worldSize = Vector3.Scale(barrier_collider.size, barrier_collider.transform.lossyScale);
            //Debug.Log($"Current world size of collider: {worldSize}");

            // The collider is too thin (depth of 1 unit) - let's make it thicker for better detection
            // We need to account for the scaling when setting the size
            Vector3 parentScale = barrier_collider.transform.lossyScale;

            Vector3 newSize = new Vector3(
                barrier_collider.size.x, // Keep width (already 10 units in world space)
                barrier_collider.size.y, // Keep height (already 8 units in world space)  
                Mathf.Max(barrier_collider.size.z, 3f / parentScale.z) // Make it at least 3 units thick in world space
            );

            barrier_collider.size = newSize;

            Vector3 newWorldSize = Vector3.Scale(barrier_collider.size, barrier_collider.transform.lossyScale);
            //Debug.Log($"Adjusted collider size from {barrier_collider.size} to {newSize}");
            //Debug.Log($"New world size: {newWorldSize}");
        }

        // Always ensure it's a trigger for detection
        barrier_collider.isTrigger = true;

        // If the collider is on a child object, add a TriggerForwarder to forward events to this parent script
        if (barrier_collider.gameObject != gameObject)
        {
            //Debug.Log("Collider is on child object - adding TriggerForwarder");
            TriggerForwarder forwarder = barrier_collider.GetComponent<TriggerForwarder>();
            if (forwarder == null)
            {
                forwarder = barrier_collider.gameObject.AddComponent<TriggerForwarder>();
                //Debug.Log("Added TriggerForwarder to child collider");
            }
            else
            {
                //Debug.Log("TriggerForwarder already exists on child collider");
            }
        }

        //Debug.Log($"Final collider setup: Size={barrier_collider.size}, Center={barrier_collider.center}, IsTrigger={barrier_collider.isTrigger}");

        // Call debug info after setup
        DebugWaterfallInfo();
    }

    public void DebugWaterfallInfo()
    {
        //Debug.Log($"=== WATERFALL DEBUG INFO for {gameObject.name} ===");
        //Debug.Log($"Position: {transform.position}");
        //Debug.Log($"Rotation: {transform.rotation.eulerAngles}");
        //Debug.Log($"Scale: {transform.localScale}");
        //Debug.Log($"Layer: {LayerMask.LayerToName(gameObject.layer)} (index: {gameObject.layer})");
        //Debug.Log($"Active: {gameObject.activeInHierarchy}");
        //Debug.Log($"Player Layer Mask: {player_layer.value}");

        if (barrier_collider != null)
        {
            //Debug.Log($"Collider GameObject: {barrier_collider.gameObject.name}");
            //Debug.Log($"Collider - Size: {barrier_collider.size}, Center: {barrier_collider.center}");
            //Debug.Log($"Collider - IsTrigger: {barrier_collider.isTrigger}, Enabled: {barrier_collider.enabled}");
            //Debug.Log($"Collider - Bounds: {barrier_collider.bounds}");
            //Debug.Log($"Collider - World Position: {barrier_collider.transform.position}");
            //Debug.Log($"Collider - Layer: {LayerMask.LayerToName(barrier_collider.gameObject.layer)} (index: {barrier_collider.gameObject.layer})");

            // Check if Physics settings allow triggers
            //Debug.Log($"Physics.queriesHitTriggers: {Physics.queriesHitTriggers}");
        }
        else
        {
            //Debug.LogError("No barrier_collider found!");
        }

        // Test overlap detection right now
        TestOverlapDetection();
    }

    private void TestOverlapDetection()
    {
        if (barrier_collider == null) return;

        //Debug.Log("=== TESTING OVERLAP DETECTION ===");

        // Test with a large area around the waterfall
        Collider[] nearbyColliders = Physics.OverlapBox(
            barrier_collider.bounds.center,
            barrier_collider.bounds.size * 1.5f,
            barrier_collider.transform.rotation
        );

        //Debug.Log($"Found {nearbyColliders.Length} colliders near waterfall:");
        foreach (Collider col in nearbyColliders)
        {
            //Debug.Log($"  - {col.gameObject.name} (Tag: {col.tag}, Layer: {LayerMask.LayerToName(col.gameObject.layer)})");
            //Debug.Log($"    Has ThirdPersonController: {col.GetComponent<ThirdPersonController>() != null}");
            //Debug.Log($"    Has CharacterController: {col.GetComponent<CharacterController>() != null}");
            //Debug.Log($"    Distance from waterfall: {Vector3.Distance(transform.position, col.transform.position)}");
        }
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

        audio_source.loop = true;
        audio_source.volume = 0.3f;
        if (audio_source.clip == null)
        {
            // You can assign a looping waterfall ambient sound here
        }
        audio_source.Play();
    }

    // Methods to handle trigger events forwarded from child colliders
    public void OnChildTriggerEnter(Collider other)
    {
        //Debug.Log($"*** CHILD TRIGGER ENTER *** Object '{other.gameObject.name}' entered child trigger!");
        //Debug.Log($"  - Tag: {other.tag}");
        //Debug.Log($"  - Layer: {LayerMask.LayerToName(other.gameObject.layer)}");
        //Debug.Log($"  - IsPlayer check result: {IsPlayerByComponents(other.gameObject)}");

        HandlePlayerInteraction(other.gameObject);
    }

    public void OnChildTriggerStay(Collider other)
    {
        if (Time.frameCount % 30 == 0) // Every 30 frames
        {
            //Debug.Log($"*** CHILD TRIGGER STAY *** Object '{other.gameObject.name}' staying in child trigger");
        }
    }

    public void OnChildTriggerExit(Collider other)
    {
        //Debug.Log($"*** CHILD TRIGGER EXIT *** Object '{other.gameObject.name}' exited child trigger");
    }

    void OnTriggerEnter(Collider other)
    {
        //Debug.Log($"*** TRIGGER ENTER *** Object '{other.gameObject.name}' entered waterfall trigger!");
        //Debug.Log($"  - Tag: {other.tag}");
        //Debug.Log($"  - Layer: {LayerMask.LayerToName(other.gameObject.layer)}");
        //Debug.Log($"  - Has ThirdPersonController: {other.GetComponent<ThirdPersonController>() != null}");
        //Debug.Log($"  - Has CharacterController: {other.GetComponent<CharacterController>() != null}");
        //Debug.Log($"  - IsPlayer check result: {IsPlayerByComponents(other.gameObject)}");

        HandlePlayerInteraction(other.gameObject);
    }

    void OnTriggerStay(Collider other)
    {
        // More frequent logging to see if Stay is working
        if (Time.frameCount % 30 == 0) // Every 30 frames
        {
            //Debug.Log($"*** TRIGGER STAY *** Object '{other.gameObject.name}' staying in waterfall trigger");
            if (IsPlayerByComponents(other.gameObject))
            {
                //Debug.Log("  - This is the player!");
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        //Debug.Log($"*** TRIGGER EXIT *** Object '{other.gameObject.name}' exited waterfall trigger");
    }

    void OnCollisionEnter(Collision collision)
    {
        //Debug.Log($"Waterfall: Collision with {collision.gameObject.name}");
        HandlePlayerInteraction(collision.gameObject);
    }

    void Update()
    {
        // Manual test - press T to test detection
        if (Input.GetKeyDown(KeyCode.T))
        {
            //Debug.Log("=== MANUAL DETECTION TEST ===");
            TestOverlapDetection();

            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);
                //Debug.Log($"Manual test: Player found at distance {distance}");
                if (distance < 5f)
                {
                    //Debug.Log("Player is close enough - testing interaction");
                    HandlePlayerInteraction(player);
                }
            }
        }

        // More frequent player detection since triggers might not be working
        if (Time.frameCount % 10 == 0) // Check every 10 frames
        {
            CheckForNearbyPlayers();
        }

        // Debug info less frequently
        if (Time.frameCount % 1800 == 0) // Every 30 seconds at 60fps
        {
            //Debug.Log($"Waterfall {gameObject.name} is active and checking for players...");
        }
    }

    private void CheckForNearbyPlayers()
    {
        // Use a larger detection area
        Vector3 detectionSize = new Vector3(barrier_thickness * 2f, 3f, 2f);
        Collider[] nearbyColliders = Physics.OverlapBox(
            transform.position,
            detectionSize * 0.5f,
            transform.rotation,
            player_layer
        );

        bool foundPlayer = false;
        foreach (Collider col in nearbyColliders)
        {
            if (IsPlayerByComponents(col.gameObject))
            {
                foundPlayer = true;

                // Only process if not in cooldown
                if (Time.time - last_bounce_time >= bounce_cooldown)
                {
                    HandlePlayerInteraction(col.gameObject);
                }
                break;
            }
        }

        // Also try finding by tag as backup
        if (!foundPlayer)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance < 3f) // Within 3 units
                {
                    if (Time.time - last_bounce_time >= bounce_cooldown)
                    {
                        HandlePlayerInteraction(player);
                    }
                }
            }
        }
    }

    private void HandlePlayerInteraction(GameObject playerObject)
    {
        if (!IsPlayerByComponents(playerObject)) return;

        ThirdPersonController thirdPersonController = playerObject.GetComponent<ThirdPersonController>();
        if (thirdPersonController == null) return;

        if (Time.time - last_bounce_time < bounce_cooldown) return;

        bool isSprinting = IsPlayerSprinting(thirdPersonController);

        if (isSprinting)
        {
            HandleSprintThrough();
        }
        else
        {
            BouncePlayerBack(playerObject);
        }
    }

    private bool IsPlayerSprinting(ThirdPersonController controller)
    {
        StarterAssetsInputs input = controller.GetComponent<StarterAssetsInputs>();
        if (input == null) return false;
        return input.sprint;
    }

    private void HandleSprintThrough()
    {
        //Debug.Log("Player sprinted through waterfall!");

        if (audio_source != null && rush_through_sound != null)
        {
            audio_source.PlayOneShot(rush_through_sound);
        }

        StartCoroutine(ShowSuccessEffect());
    }

    private void BouncePlayerBack(GameObject playerObject)
    {
        last_bounce_time = Time.time;

        if (player_movement_disabled) return;

        Vector3 bounce_direction = (playerObject.transform.position - transform.position).normalized;
        bounce_direction.y = 0;
        bounce_direction = bounce_direction.normalized;

        float pushback_distance = 3f;
        Vector3 new_position = playerObject.transform.position + (bounce_direction * pushback_distance);
        new_position.y = playerObject.transform.position.y;

        playerObject.transform.position = new_position;

        ThirdPersonController controller = playerObject.GetComponent<ThirdPersonController>();
        if (controller != null && !player_movement_disabled)
        {
            StartCoroutine(DisableMovementBriefly(controller));
        }

        if (audio_source != null && bounce_sound != null)
        {
            audio_source.PlayOneShot(bounce_sound);
        }

        StartCoroutine(ShowBounceEffect());

        //Debug.Log($"Player bounced back to: {new_position}");
    }

    private IEnumerator DisableMovementBriefly(ThirdPersonController controller)
    {
        if (player_movement_disabled) yield break;

        player_movement_disabled = true;

        bool originalEnabled = controller.enabled;
        controller.enabled = false;

        yield return new WaitForSeconds(0.3f);

        controller.enabled = originalEnabled;
        player_movement_disabled = false;
    }

    private IEnumerator ShowSuccessEffect()
    {
        yield return new WaitForSeconds(0.2f);
    }

    private IEnumerator ShowBounceEffect()
    {
        if (waterfall_effect != null)
        {
            // You could change the material color briefly or add extra particles
        }

        yield return new WaitForSeconds(0.3f);
    }

    private bool IsPlayerByComponents(GameObject obj)
    {
        bool hasController = obj.GetComponent<ThirdPersonController>() != null;
        bool hasCharacterController = obj.GetComponent<CharacterController>() != null;
        bool nameMatch = obj.name.ToLower().Contains("player");
        bool tagMatch = obj.CompareTag("Player");

        // Only log when we find a potential player to reduce spam
        if (hasController || hasCharacterController || nameMatch || tagMatch)
        {
            //Debug.Log($"Player detection for '{obj.name}': Controller={hasController}, CharController={hasCharacterController}, NameMatch={nameMatch}, TagMatch={tagMatch}");
        }

        return hasController || hasCharacterController || nameMatch || tagMatch;
    }

    private bool IsPlayer(Collider other)
    {
        return IsPlayerByComponents(other.gameObject);
    }

    void OnDrawGizmos()
    {
        // Draw the actual collider bounds
        if (barrier_collider != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.matrix = barrier_collider.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(barrier_collider.center, barrier_collider.size);

            // Reset matrix for other gizmos
            Gizmos.matrix = Matrix4x4.identity;

            // Draw world bounds
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(barrier_collider.bounds.center, barrier_collider.bounds.size);
        }
        else
        {
            // Fallback if no collider found yet
            Gizmos.color = Color.red;
            Vector3 gizmoSize = new Vector3(barrier_thickness, 3f, 1f);
            Gizmos.DrawWireCube(transform.position, gizmoSize);
        }

        // Draw direction arrow
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * 2f);

        // Draw detection area
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, new Vector3(6f, 4f, 4f)); // Detection area
    }
}