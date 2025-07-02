using UnityEngine;

public class WaterfallController : MonoBehaviour
{
    [Header("Waterfall Settings")]
    public ParticleSystem waterfallParticles;
    public AudioSource waterfallAudio;
    public AudioClip waterfallSound;

    [Header("Force Settings")]
    public float pushForce = 15f;
    public Vector3 flowDirection = Vector3.down;

    [Header("Sprint Requirement")]
    public bool requiresSprint = true;
    public float minimumSpeedRequired = 10f;

    private void Start()
    {
        // Setup audio
        if (waterfallAudio && waterfallSound)
        {
            waterfallAudio.clip = waterfallSound;
            waterfallAudio.loop = true;
            waterfallAudio.Play();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player entered waterfall!");
        }
    }

    private void OnTriggerStay(Collider other)
    {
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
                // Push player back - they're not fast enough!
                Vector3 pushBack = -flowDirection.normalized * pushForce * Time.deltaTime;

                if (playerRb)
                    playerRb.AddForce(pushBack, ForceMode.Force);
                else if (playerController)
                {
                    // For CharacterController, apply pushback directly
                    playerController.Move(pushBack);
                    Debug.Log("Player too slow! Being pushed back!");
                }
            }
            else
            {
                // Player is fast enough, let them pass
                Debug.Log("Player successfully passing through waterfall!");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player exited waterfall!");
        }
    }
}