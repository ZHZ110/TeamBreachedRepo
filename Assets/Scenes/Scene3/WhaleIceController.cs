using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using StarterAssets;
using Cinemachine;

public class WhaleIceController : MonoBehaviour
{
    [Header("Ice Mechanics")]
    public float jumpForce = 12f;
    public float forwardJumpForce = 8f;
    public float wiggleForce = 5f;
    public float slideFriction = 0.1f;

    [Header("Water Settings")]
    public float waterBobForce = 2f;
    public float waterDamping = 5f;

    private Rigidbody rb;
    private bool isGrounded = false;
    private bool isInWater = false;
    private StarterAssetsInputs _input;
    private WhaleStaminaSystem staminaSystem;

    // Scene transition tracking
    private bool hasJumped = false;
    private bool hasReturnedToWater = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        _input = GetComponent<StarterAssetsInputs>();
        staminaSystem = GetComponent<WhaleStaminaSystem>();

        // Completely disable camera look input
        if (_input != null)
        {
            _input.look = Vector2.zero;
        }

        // Lock cursor manually
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Start gently - reduce initial velocity
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Set up slippery material for ice
        if (GetComponent<Collider>().material == null)
        {
            PhysicsMaterial slipperyMaterial = new PhysicsMaterial("Slippery");
            slipperyMaterial.dynamicFriction = slideFriction;
            slipperyMaterial.staticFriction = slideFriction;
            GetComponent<Collider>().material = slipperyMaterial;
        }
    }

    void Update()
    {
        // Force look input to stay zero
        if (_input != null)
        {
            _input.look = Vector2.zero;
        }

        HandleStaminaInput();
        HandleMovement();
    }

    private void HandleStaminaInput()
    {
        // Check for stamina usage (equivalent to W key from original script)
        bool isUsingStamina = _input.sprint || _input.move.magnitude > 0.5f;
        staminaSystem.SetStaminaUsage(isUsingStamina);

        // Check for cooling (equivalent to S key from original script)
        // You could map this to a specific key or action
        bool isCooling = Input.GetKey(KeyCode.S); // Keep S key for cooling
        staminaSystem.SetCooling(isCooling);
    }

    private void HandleMovement()
    {
        // Gentle water bobbing when in water
        if (isInWater && !_input.jump)
        {
            rb.AddForce(Vector3.up * waterBobForce, ForceMode.Force);
            rb.linearVelocity *= (1 - waterDamping * Time.deltaTime);
        }

        // Only jump when player presses jump AND has stamina
        if (_input.jump && (isGrounded || isInWater) && staminaSystem.HasStamina())
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

            // Add upward force
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

            // Add forward force in the direction whale is facing
            rb.AddForce(transform.forward * forwardJumpForce, ForceMode.Impulse);

            // Mark that whale has jumped
            hasJumped = true;
            Debug.Log("Whale has jumped! Watching for return to water...");

            _input.jump = false;
        }
        else if (_input.jump && !staminaSystem.HasStamina())
        {
            // No stamina feedback
            Debug.Log("Not enough stamina to jump!");
            _input.jump = false;
        }

        // Wiggle controls - also require stamina
        if (staminaSystem.HasStamina())
        {
            if (_input.move.x < -0.1f)
            {
                rb.AddForce(Vector3.left * wiggleForce, ForceMode.Force);
            }
            if (_input.move.x > 0.1f)
            {
                rb.AddForce(Vector3.right * wiggleForce, ForceMode.Force);
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"Collision detected with: {collision.gameObject.name}, Tag: {collision.gameObject.tag}");

        if (collision.gameObject.name.Contains("Water") || collision.gameObject.tag == "Water")
        {
            Debug.Log("Recognized as water!");
            isInWater = true;
            isGrounded = true;

            // Check if whale has jumped and this is return to water
            if (hasJumped && !hasReturnedToWater)
            {
                hasReturnedToWater = true;
                Debug.Log("Whale returned to water after jumping! Loading Scene 4...");

                // Add small delay for dramatic effect (optional)
                Invoke("LoadScene4", 1.0f);
            }
            else if (hasJumped && hasReturnedToWater)
            {
                Debug.Log("Already returned to water, not triggering again");
            }
            else if (!hasJumped)
            {
                Debug.Log("Whale hasn't jumped yet");
            }
        }
        else
        {
            Debug.Log("Not recognized as water");
            isInWater = false;
            isGrounded = true;
        }
    }

    private void LoadScene4()
    {
        // Unlock cursor before switching scenes
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene("Scene4");
    }

    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.name.Contains("Water") || collision.gameObject.tag == "Water")
        {
            isInWater = true;
        }
        isGrounded = true;
    }

    void OnCollisionExit(Collision collision)
    {
        isGrounded = false;
        isInWater = false;
    }
}