using UnityEngine;
using StarterAssets;

public class WhaleIceController : MonoBehaviour
{
    [Header("Ice Mechanics")]
    public float jumpForce = 12f;
    public float wiggleForce = 5f;
    public float slideFriction = 0.1f;

    [Header("Water Settings")]
    public float waterBobForce = 2f;
    public float waterDamping = 5f;

    private Rigidbody rb;
    private bool isGrounded = false;
    private bool isInWater = false;
    private StarterAssetsInputs _input;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        _input = GetComponent<StarterAssetsInputs>();

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
        // Gentle water bobbing when in water
        if (isInWater && !_input.jump)
        {
            // Keep whale floating at water surface with gentle bobbing
            rb.AddForce(Vector3.up * waterBobForce, ForceMode.Force);
            rb.linearVelocity *= (1 - waterDamping * Time.deltaTime); // Dampen movement
        }

        // Only jump when player presses jump
        if (_input.jump && (isGrounded || isInWater))
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z); // Stop vertical movement first
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            _input.jump = false;
        }

        // Wiggle controls
        if (_input.move.x < -0.1f)
        {
            rb.AddForce(Vector3.left * wiggleForce, ForceMode.Force);
        }
        if (_input.move.x > 0.1f)
        {
            rb.AddForce(Vector3.right * wiggleForce, ForceMode.Force);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.name.Contains("Water") || collision.gameObject.tag == "Water")
        {
            isInWater = true;
            isGrounded = true;
        }
        else
        {
            isInWater = false;
            isGrounded = true;
        }
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