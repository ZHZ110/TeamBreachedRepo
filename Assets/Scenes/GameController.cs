using UnityEngine;
using UnityEngine.SceneManagement;
using StarterAssets;

public class GameController : MonoBehaviour
{
    [Header("Controllers")]
    public ThirdPersonController swimController; // Unity's controller
    public WhaleIceController iceController;     // Your custom controller
    public WhaleStaminaSystem staminaSystem;

    void Start()
    {
        // Get components from the same GameObject
        swimController = GetComponent<ThirdPersonController>();
        iceController = GetComponent<WhaleIceController>();
        staminaSystem = GetComponent<WhaleStaminaSystem>();

        // Stamina system is always active
        staminaSystem.enabled = true;

        // Scene 2: Swimming
        if (SceneManager.GetActiveScene().name == "Scene2")
        {
            swimController.enabled = true;
            iceController.enabled = false;
            swimController.FloatingMode = true;  // Perfect for whale swimming!
        }
        // Scene 3: Ice mechanics
        else if (SceneManager.GetActiveScene().name == "Scene3")
        {
            swimController.enabled = false;
            iceController.enabled = true;
        }
    }
}