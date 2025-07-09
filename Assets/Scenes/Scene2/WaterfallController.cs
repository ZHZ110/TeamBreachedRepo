using UnityEngine;

public class WaterfallController : MonoBehaviour
{
    [Header("Waterfall Settings")]
    public ParticleSystem waterfallParticles;
    public AudioSource waterfallAudio; // Use dedicated AudioSource for waterfall
    public AudioClip waterfallSound;

    [Header("Force Settings")]
    public float pushForce = 15f;
    public Vector3 flowDirection = Vector3.down;

    [Header("Sprint Requirement")]
    public bool requiresSprint = true;
    public float minimumSpeedRequired = 10f;

    [Header("Audio Distance Settings")]
    public float maxHearingDistance = 20f;
    public float maxVolumeDistance = 5f;
    [Range(0f, 1f)]
    public float waterfallSoundVolume = 1f;

    private Transform player;
    private bool playerInRange = false;

    private void Start()
    {
        // Create dedicated AudioSource if not assigned
        if (waterfallAudio == null)
        {
            // Create a child GameObject for waterfall audio to avoid conflicts
            GameObject waterfallAudioGO = new GameObject("WaterfallAudio");
            waterfallAudioGO.transform.SetParent(transform);
            waterfallAudioGO.transform.localPosition = Vector3.zero;
            waterfallAudio = waterfallAudioGO.AddComponent<AudioSource>();
        }

        // Setup audio but don't play yet
        if (waterfallAudio && waterfallSound)
        {
            waterfallAudio.clip = waterfallSound;
            waterfallAudio.loop = true;
            waterfallAudio.volume = 0f;
            waterfallAudio.playOnAwake = false; // Important!

            // Set up 3D audio settings
            waterfallAudio.spatialBlend = 1f;
            waterfallAudio.rolloffMode = AudioRolloffMode.Linear;
            waterfallAudio.minDistance = maxVolumeDistance;
            waterfallAudio.maxDistance = maxHearingDistance;

            //Debug.Log($"Waterfall audio setup complete. Clip: {waterfallSound.name}");
        }
        else
        {
            //Debug.LogWarning("Waterfall audio or sound clip not assigned!");
        }
    }

    private void Update()
    {
        // Always check distance to player for audio, regardless of trigger
        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            player = playerGO.transform;
            UpdateAudioVolume();
        }

        // Original trigger-based logic for other mechanics
        if (playerInRange && player != null)
        {
            // Any other waterfall effects that need trigger entry
        }

        // Debug: Show if we can find the player at all
        if (Input.GetKeyDown(KeyCode.F1)) // Press F1 to debug
        {
            if (playerGO != null)
            {
                //Debug.Log($"Found player: {playerGO.name} at position {playerGO.transform.position}");
                CharacterController cc = playerGO.GetComponent<CharacterController>();
                //Debug.Log($"Player has CharacterController: {cc != null}");
                if (cc != null)
                {
                    //Debug.Log($"CharacterController center: {cc.center}, radius: {cc.radius}, height: {cc.height}");
                }

                float distanceToPlayer = Vector3.Distance(transform.position, playerGO.transform.position);
                //Debug.Log($"Distance to player: {distanceToPlayer}");
            }
            else
            {
                //Debug.LogError("No GameObject with 'Player' tag found!");
            }
        }
    }

    private void UpdateAudioVolume()
    {
        if (waterfallAudio == null || waterfallSound == null)
        {
            //Debug.LogWarning("Waterfall audio or sound clip is null!");
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);
        float volumePercent = 0f;

        //Debug.Log($"Waterfall: distance={distance:F2}, maxVolumeDistance={maxVolumeDistance}, maxHearingDistance={maxHearingDistance}");

        if (distance <= maxVolumeDistance)
        {
            volumePercent = 1f;
            //Debug.Log("Waterfall: At max volume");
        }
        else if (distance <= maxHearingDistance)
        {
            volumePercent = 1f - ((distance - maxVolumeDistance) / (maxHearingDistance - maxVolumeDistance));
            //Debug.Log($"Waterfall: Fading volume to {volumePercent:F2}");
        }
        else
        {
            //Debug.Log("Waterfall: Too far away, no sound");
        }

        // Apply volume
        waterfallAudio.volume = volumePercent * waterfallSoundVolume;
        //Debug.Log($"Waterfall: Final volume set to {waterfallAudio.volume:F2}");

        // Start or stop audio based on volume
        if (volumePercent > 0f && !waterfallAudio.isPlaying)
        {
            waterfallAudio.Play();
            //Debug.Log("★ WATERFALL AUDIO STARTED ★");
        }
        else if (volumePercent <= 0f && waterfallAudio.isPlaying)
        {
            waterfallAudio.Stop();
            //Debug.Log("Waterfall audio stopped");
        }
        else if (volumePercent > 0f && waterfallAudio.isPlaying)
        {
            //Debug.Log("Waterfall audio already playing, just updated volume");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log($"Waterfall trigger entered by: {other.name} with tag: {other.tag}");

        if (other.CompareTag("Player"))
        {
            //Debug.Log("★ PLAYER ENTERED WATERFALL AREA! ★");
            player = other.transform;
            playerInRange = true;
            UpdateAudioVolume();
        }
        else
        {
            //Debug.Log($"Not player - ignoring {other.name}");
        }
    }

    private void OnTriggerStay(Collider other)
    {
        //Debug.Log($"Waterfall trigger STAY: {other.name} with tag: {other.tag}");

        if (other.CompareTag("Player"))
        {
            // Get player's current speed
            Rigidbody playerRb = other.GetComponent<Rigidbody>();
            CharacterController playerController = other.GetComponent<CharacterController>();
            float currentSpeed = 0f;

            if (playerRb)
                currentSpeed = playerRb.linearVelocity.magnitude;
            else if (playerController)
                currentSpeed = playerController.velocity.magnitude;

            // Check if player meets speed requirement
            if (requiresSprint && currentSpeed < minimumSpeedRequired)
            {
                Vector3 pushBack = -flowDirection.normalized * pushForce * Time.deltaTime;
                if (playerRb)
                    playerRb.AddForce(pushBack, ForceMode.Force);
                else if (playerController)
                {
                    playerController.Move(pushBack);
                    //Debug.Log("Player too slow! Being pushed back!");
                }
            }
            else
            {
                //Debug.Log("Player successfully passing through waterfall!");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            //Debug.Log("Player exited waterfall area!");
            playerInRange = false;
            player = null;

            if (waterfallAudio && waterfallAudio.isPlaying)
            {
                waterfallAudio.Stop();
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, maxHearingDistance);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, maxVolumeDistance);
    }
}