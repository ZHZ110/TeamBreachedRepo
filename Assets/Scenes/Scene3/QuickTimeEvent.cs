using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class QuickTimeEvent : MonoBehaviour
{
    [Header("UI References")]
    public GameObject qtePanel;
    public Slider progressBar;
    public TextMeshProUGUI instructionText;
    public GameObject spacebarIndicator; // Optional visual element to show spacebar

    [Header("Info Window")]
    public GameObject infoWindow;
    public TextMeshProUGUI infoText;
    public TextMeshProUGUI continuePrompt;

    [Header("QTE Settings")]
    public float qteDuration = 3f;
    public KeyCode qteKey = KeyCode.Space;
    public int requiredPresses = 5;

    [Header("Whale References")]
    public GameObject whale;
    public WhaleStaminaSystem staminaSystem; // Assign this in inspector
    public Transform jumpTarget;
    public float jumpForce = 12f;
    public float forwardJumpForce = 8f;
    public float lungeForce = 10f; // Force for the seal lunge

    [Header("Seal References")]
    public GameObject seal;
    public float sealKnockbackForce = 15f;

    [Header("Info Window Settings")]
    [TextArea(3, 5)]
    public string informationText = "We finally caught up to the seal! Since it's on the iceberg, we should try breaching. Breaching is when you intentionally jump onto land in order to hunt. Movements can take a lot of energy so keep an eye on your stamina bar. Things won't go well if you run out of energy out of the water.";
    public string promptText = "When you are ready, press SPACE to start";

    [TextArea(3, 5)]
    public string successInfoText = "Good job! You need to be careful about getting too far onto land when you breach because you could get stuck. Let's lunge at the seal and try to knock it into the water.";
    public string successPromptText = "Press SPACE to lunge at the seal";

    [TextArea(3, 5)]
    public string finalInfoText = "You did it! The rest of the pod will get the seal. Let's get you back into the water. You'll need to wiggle back and forth to move back into the water.";
    public string finalPromptText = "Press W to wiggle";

    [TextArea(3, 5)]
    public string completionText = "Now you know how to breach. Let's go eat!";
    public string completionPromptText = "Press SPACE to continue";

    [Header("Stamina Settings")]
    public int jumpStaminaCost = 500; // Direct stamina amount to remove
    public int lungeStaminaCost = 250; // Direct stamina amount to remove
    public int wiggleStaminaCost = 50; // Direct stamina amount to remove

    [Header("Wiggle Settings")]
    public float wiggleBackwardForce = 3f; // How much force each W press applies backwards

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip successSound;
    public AudioClip failSound;
    public AudioClip pressSound;
    public AudioClip jumpSound; // NEW: Specific sound for when whale jumps
    public AudioClip lungeSound; // NEW: Specific sound for when whale lunges
    public AudioClip wiggleSound; // NEW: Specific sound for wiggling (optional)
    public AudioClip advanceSound; // NEW: Sound for advancing through tutorial dialogue

    private bool qteActive = false;
    private bool infoWindowActive = false;
    private bool isSecondStage = false; // Track if we're on the second info stage
    private bool isFinalStage = false; // Track if we're on the final info stage
    private bool isCompletionStage = false; // Track if we're showing completion message
    private bool wiggleMode = false; // Track if whale is in wiggle-back mode
    private float qteTimer;
    private int currentPresses = 0;
    private bool qteCompleted = false;
    private Rigidbody whaleRigidbody;
    private Coroutine currentStaminaDrain; // Track current stamina drain coroutine

    void Start()
    {
        // Hide QTE panel initially, but show info window
        if (qtePanel) qtePanel.SetActive(false);

        // Get whale components
        if (whale)
        {
            whaleRigidbody = whale.GetComponent<Rigidbody>();

            // If stamina system not assigned, try to find it on the whale
            if (staminaSystem == null)
            {
                staminaSystem = whale.GetComponent<WhaleStaminaSystem>();
            }
        }

        // Initialize progress bar
        if (progressBar) progressBar.value = 0f;

        // Setup info window text (initial stage)
        if (infoText) infoText.text = informationText;
        if (continuePrompt) continuePrompt.text = promptText;

        // Show info window immediately when scene loads
        ShowInfoWindow();
    }

    void Update()
    {
        if (infoWindowActive)
        {
            // Wait for appropriate key press based on stage
            if (isCompletionStage)
            {
                // Completion stage - wait for space to go to Scene 4
                if (Input.GetKeyDown(qteKey))
                {
                    // Play advance sound
                    if (audioSource && advanceSound)
                        audioSource.PlayOneShot(advanceSound);

                    LoadScene4();
                }
            }
            else if (isFinalStage)
            {
                // Final stage - wait for W key to start wiggling
                if (Input.GetKeyDown(KeyCode.W))
                {
                    // Play advance sound
                    if (audioSource && advanceSound)
                        audioSource.PlayOneShot(advanceSound);

                    HandleWiggleStart();
                }
            }
            else if (Input.GetKeyDown(qteKey))
            {
                // Play advance sound for tutorial progression
                if (audioSource && advanceSound)
                    audioSource.PlayOneShot(advanceSound);

                if (isSecondStage)
                {
                    // Second stage - handle seal lunge
                    HandleSealLunge();
                }
                else
                {
                    // First stage - start breach QTE
                    StartQuickTimeEvent();
                }
            }
        }
        else if (qteActive && !qteCompleted)
        {
            HandleQTEInput();
            UpdateQTETimer();
            UpdateUI();
        }
        else if (wiggleMode)
        {
            // Handle wiggle movement
            if (Input.GetKeyDown(KeyCode.W))
            {
                PerformWiggleBack();
            }
        }
    }

    public void ShowInfoWindow()
    {
        if (infoWindowActive || qteActive) return;

        infoWindowActive = true;

        // Show info window
        if (infoWindow) infoWindow.SetActive(true);

        Debug.Log("Info window displayed. Press Space to continue...");
    }

    public void StartQuickTimeEvent()
    {
        if (qteActive) return;

        // Hide info window and show QTE
        infoWindowActive = false;
        if (infoWindow) infoWindow.SetActive(false);

        qteActive = true;
        qteCompleted = false;
        qteTimer = qteDuration;
        currentPresses = 0;

        // Show QTE UI
        if (qtePanel) qtePanel.SetActive(true);

        // Update instruction text
        if (instructionText)
            instructionText.text = $"Press {qteKey} rapidly! ({requiredPresses} times)";

        Debug.Log("Quick Time Event Started!");
    }

    void HandleQTEInput()
    {
        if (Input.GetKeyDown(qteKey))
        {
            OnQTEKeyPress();
        }
    }

    public void OnQTEKeyPress()
    {
        if (!qteActive || qteCompleted) return;

        // Optional: Flash spacebar indicator for visual feedback
        if (spacebarIndicator)
        {
            StartCoroutine(FlashIndicator());
        }

        currentPresses++;

        // Play press sound
        if (audioSource && pressSound)
            audioSource.PlayOneShot(pressSound);

        Debug.Log($"QTE Press: {currentPresses}/{requiredPresses}");

        // Check if QTE is completed successfully
        if (currentPresses >= requiredPresses)
        {
            CompleteQTE(true);
        }
    }

    IEnumerator FlashIndicator()
    {
        if (!spacebarIndicator) yield break;

        // Quick flash effect
        var originalColor = spacebarIndicator.GetComponent<UnityEngine.UI.Image>().color;
        spacebarIndicator.GetComponent<UnityEngine.UI.Image>().color = Color.yellow;
        yield return new WaitForSeconds(0.1f);
        spacebarIndicator.GetComponent<UnityEngine.UI.Image>().color = originalColor;
    }

    void UpdateQTETimer()
    {
        qteTimer -= Time.deltaTime;

        if (qteTimer <= 0f && !qteCompleted)
        {
            CompleteQTE(false);
        }
    }

    void UpdateUI()
    {
        // Update progress bar to show time remaining
        if (progressBar)
        {
            float timeProgress = qteTimer / qteDuration;
            progressBar.value = timeProgress;
        }
    }

    void CompleteQTE(bool success)
    {
        qteActive = false;
        qteCompleted = true;

        // Hide QTE UI
        if (qtePanel) qtePanel.SetActive(false);

        if (success)
        {
            Debug.Log("QTE Success!");

            // Play success sound
            if (audioSource && successSound)
                audioSource.PlayOneShot(successSound);

            if (isSecondStage)
            {
                // Second stage success - perform lunge
                PerformWhaleLunge();

                // After successful lunge, show final info after a delay
                Invoke("ShowFinalInfo", 2f);
            }
            else
            {
                // First stage success - perform jump
                PerformWhaleJump();

                // After successful jump, show success info after a delay
                Invoke("ShowSuccessInfo", 2f);
            }
        }
        else
        {
            Debug.Log("QTE Failed! Returning to info window...");

            // Play fail sound
            if (audioSource && failSound)
                audioSource.PlayOneShot(failSound);

            if (isSecondStage)
            {
                // Failed lunge - return to success info
                Invoke("ShowSuccessInfo", 1.5f);
            }
            else
            {
                // Failed breach - return to initial info
                Invoke("ReturnToInfoWindow", 1.5f);
            }
        }
    }

    void PerformWhaleJump()
    {
        if (!whaleRigidbody) return;

        // Check stamina if system exists
        if (staminaSystem && !staminaSystem.HasStamina())
        {
            Debug.Log("Not enough stamina to jump!");
            return;
        }

        // Consume stamina for jump - direct reduction
        if (staminaSystem)
        {
            ReduceStaminaDirectly(jumpStaminaCost, "Jump");
        }

        // Reset vertical velocity like in your original script
        whaleRigidbody.linearVelocity = new Vector3(whaleRigidbody.linearVelocity.x, 0, whaleRigidbody.linearVelocity.z);

        // Add upward force
        whaleRigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

        // Add forward force in the direction whale is facing
        whaleRigidbody.AddForce(whale.transform.forward * forwardJumpForce, ForceMode.Impulse);

        // NEW: Play jump sound when whale jumps
        if (audioSource && jumpSound)
            audioSource.PlayOneShot(jumpSound);

        Debug.Log("Whale jumped using physics forces!");
    }

    void PerformWhaleLunge()
    {
        if (!whaleRigidbody) return;

        // Check stamina if system exists
        if (staminaSystem && !staminaSystem.HasStamina())
        {
            Debug.Log("Not enough stamina to lunge!");
            return;
        }

        // Consume stamina for lunge - direct reduction
        if (staminaSystem)
        {
            ReduceStaminaDirectly(lungeStaminaCost, "Lunge");
        }

        // Make whale lunge forward
        whaleRigidbody.AddForce(whale.transform.forward * lungeForce, ForceMode.Impulse);

        // NEW: Play lunge sound when whale lunges
        if (audioSource && lungeSound)
            audioSource.PlayOneShot(lungeSound);

        // Make seal fly backwards
        KnockSealBackwards();

        Debug.Log("Whale lunges forward at the seal!");
    }

    void ReduceStaminaDirectly(int staminaAmount, string actionName)
    {
        if (staminaSystem == null)
        {
            Debug.LogError("Stamina system is null! Make sure it's assigned in the inspector.");
            return;
        }

        int currentStamina = staminaSystem.GetStamina();
        int newStamina = Mathf.Max(0, currentStamina - staminaAmount);

        Debug.Log($"{actionName} reducing stamina by {staminaAmount} (was {currentStamina}, now will be {newStamina})");

        // Use reflection to directly set the stamina value
        var field = typeof(WhaleStaminaSystem).GetField("currentStamina", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(staminaSystem, newStamina);
            Debug.Log($"{actionName} stamina reduction complete. Current: {staminaSystem.GetStamina()}");
        }
        else
        {
            Debug.LogError("Could not access currentStamina field. Using fallback method.");
            // Fallback to the timed drain method
            StartCoroutine(InstantStaminaDrain(staminaAmount, actionName));
        }
    }

    void RestoreFullStamina()
    {
        if (staminaSystem == null)
        {
            Debug.LogError("Stamina system is null!");
            return;
        }

        // Use reflection to directly set stamina to max value (1000)
        var field = typeof(WhaleStaminaSystem).GetField("currentStamina", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(staminaSystem, 1000); // Set to max stamina
            Debug.Log("Stamina restored to full! Current: " + staminaSystem.GetStamina());
        }
        else
        {
            Debug.LogError("Could not access currentStamina field to restore stamina.");
        }
    }

    System.Collections.IEnumerator InstantStaminaDrain(int staminaAmount, string actionName)
    {
        // Turn on stamina usage (drains 60/second, no regen while active)
        staminaSystem.SetStaminaUsage(true);

        // Calculate exact time needed - your system drains 60 per second when SetStaminaUsage is true
        // and stops regeneration, so net drain is exactly 60 per second
        float drainTime = (float)staminaAmount / 60f;

        yield return new WaitForSeconds(drainTime);

        // Turn off stamina usage (allows regeneration to resume)
        staminaSystem.SetStaminaUsage(false);

        // Clear the current drain reference
        currentStaminaDrain = null;

        Debug.Log($"{actionName} stamina reduction complete. Current: {staminaSystem.GetStamina()}");
    }

    void KnockSealBackwards()
    {
        if (!seal)
        {
            Debug.LogWarning("No seal object assigned!");
            return;
        }

        // Get seal's rigidbody
        Rigidbody sealRigidbody = seal.GetComponent<Rigidbody>();

        if (!sealRigidbody)
        {
            Debug.LogWarning("Seal needs a Rigidbody component to be knocked back!");
            return;
        }

        // Calculate direction from whale to seal
        Vector3 knockbackDirection = (seal.transform.position - whale.transform.position).normalized;

        // Add some upward force so seal flies through the air
        knockbackDirection.y = 0.5f;
        knockbackDirection = knockbackDirection.normalized;

        // Apply force to seal
        sealRigidbody.AddForce(knockbackDirection * sealKnockbackForce, ForceMode.Impulse);

        Debug.Log("Seal knocked backwards into the water!");
    }

    void ReturnToInfoWindow()
    {
        // Reset QTE state
        qteActive = false;
        qteCompleted = false;

        // Hide QTE panel
        if (qtePanel) qtePanel.SetActive(false);

        // Show info window again for retry
        ShowInfoWindow();

        Debug.Log("Ready to try breaching again!");
    }

    void ShowSuccessInfo()
    {
        // Update to second stage
        isSecondStage = true;

        // Update text to success messages
        if (infoText) infoText.text = successInfoText;
        if (continuePrompt) continuePrompt.text = successPromptText;

        // Show the info window again
        ShowInfoWindow();

        Debug.Log("Success info displayed. Ready for seal lunge!");
    }

    void ShowFinalInfo()
    {
        // Update to final stage
        isFinalStage = true;
        isSecondStage = false; // No longer in second stage

        // Update text to final messages
        if (infoText) infoText.text = finalInfoText;
        if (continuePrompt) continuePrompt.text = finalPromptText;

        // Show the info window again
        ShowInfoWindow();

        Debug.Log("Final info displayed. Press W to start wiggling back to water!");
    }

    void HandleWiggleStart()
    {
        // Hide info window
        infoWindowActive = false;
        if (infoWindow) infoWindow.SetActive(false);

        // Enable wiggle mode
        wiggleMode = true;

        Debug.Log("Wiggle mode activated! Press W repeatedly to move back to water.");
    }

    void PerformWiggleBack()
    {
        if (!whaleRigidbody) return;

        // Move whale backwards (opposite of forward direction)
        Vector3 backwardDirection = -whale.transform.forward;
        whaleRigidbody.AddForce(backwardDirection * wiggleBackwardForce, ForceMode.Impulse);

        // Play wiggle sound if available, otherwise use press sound
        if (audioSource)
        {
            if (wiggleSound)
                audioSource.PlayOneShot(wiggleSound);
            else if (pressSound)
                audioSource.PlayOneShot(pressSound);
        }

        Debug.Log("Whale wiggles backwards!");

        // Add slight stamina cost for wiggling - direct reduction
        if (staminaSystem)
        {
            ReduceStaminaDirectly(wiggleStaminaCost, "Wiggle");
        }
    }

    // Detect when whale touches water during wiggle mode
    void OnTriggerEnter(Collider other)
    {
        CheckWaterContact(other);
    }

    void OnCollisionEnter(Collision collision)
    {
        CheckWaterContact(collision.collider);
    }

    void CheckWaterContact(Collider other)
    {
        // Only check for water if we're in wiggle mode
        if (wiggleMode && other.CompareTag("Water"))
        {
            Debug.Log("Whale returned to water!");

            // Restore stamina to full when returning to water
            RestoreFullStamina();

            wiggleMode = false;
            ShowCompletionInfo();
        }
    }

    // Method to detect when whale returns to water (now handled automatically)
    public void OnReturnToWater()
    {
        if (wiggleMode)
        {
            wiggleMode = false;
            ShowCompletionInfo();
        }
    }

    void ShowCompletionInfo()
    {
        // Update to completion stage
        isCompletionStage = true;
        isFinalStage = false;

        // Update text to completion messages
        if (infoText) infoText.text = completionText;
        if (continuePrompt) continuePrompt.text = completionPromptText;

        // Show the info window again
        ShowInfoWindow();

        Debug.Log("Breaching tutorial complete! Press Space to continue to Scene 4.");
    }

    void LoadScene4()
    {
        Debug.Log("Loading Scene 4...");
        UnityEngine.SceneManagement.SceneManager.LoadScene("Scene4");
    }

    void HandleSealLunge()
    {
        // Hide info window and start the lunge QTE
        infoWindowActive = false;
        if (infoWindow) infoWindow.SetActive(false);

        // Start the lunge QTE
        StartLungeQTE();
    }

    void StartLungeQTE()
    {
        qteActive = true;
        qteCompleted = false;
        qteTimer = qteDuration;
        currentPresses = 0;

        // Show QTE UI
        if (qtePanel) qtePanel.SetActive(true);

        // Update instruction text for lunge
        if (instructionText)
            instructionText.text = $"Press {qteKey} rapidly to lunge! ({requiredPresses} times)";

        Debug.Log("Lunge QTE Started!");
    }

    // Public method to trigger the info window (call this from other scripts)
    public void TriggerJumpOpportunity()
    {
        ShowInfoWindow();
    }
}