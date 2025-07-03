using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Scene2TutorialInfo : MonoBehaviour
{
    [Header("UI References")]
    public GameObject infoWindow;
    public TextMeshProUGUI infoText;
    public TextMeshProUGUI continuePrompt;

    [Header("Player Control")]
    public MonoBehaviour playerController; // Assign your whale controller script
    public KeyCode continueKey = KeyCode.Space;

    [Header("Tutorial Messages")]
    [TextArea(3, 5)]
    public string firstMessage = "You are old enough to hunt now, so let's give it a try. I saw a seal swim that way just a minute ago. Let's go catch it.";

    [TextArea(3, 5)]
    public string secondMessage = "You can use the arrow keys to swim and Q/E to move up and down.";

    [TextArea(3, 5)]
    public string thirdMessage = "If you hold shift, you can swim faster, which is necessary to get through waterfalls. If you approach a rock and hold space, you can push rocks out of the way.";

    [TextArea(3, 5)]
    public string fourthMessage = "It can be hard navigating the ocean, so if you get lost, press C to use echolocation. If it's green, you are headed in the right direction.";

    [Header("Prompts")]
    public string continuePromptText = "Press SPACE to continue";
    public string startPromptText = "Press SPACE to start";

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip advanceSound;

    private bool infoWindowActive = false;
    private int currentStage = 0; // 0 = first message, 1 = second message, 2 = third message
    private bool tutorialComplete = false;

    void Start()
    {
        // Disable player movement initially
        DisablePlayerMovement();

        // Setup and show the first tutorial message
        SetupTutorialStage(0);
        ShowInfoWindow();
    }

    void Update()
    {
        // Only handle input if info window is active and tutorial isn't complete
        if (infoWindowActive && !tutorialComplete)
        {
            if (Input.GetKeyDown(continueKey))
            {
                AdvanceToNextStage();
            }
        }
    }

    void SetupTutorialStage(int stage)
    {
        currentStage = stage;

        switch (stage)
        {
            case 0: // First message
                if (infoText) infoText.text = firstMessage;
                if (continuePrompt) continuePrompt.text = continuePromptText;
                break;

            case 1: // Second message
                if (infoText) infoText.text = secondMessage;
                if (continuePrompt) continuePrompt.text = continuePromptText;
                break;

            case 2: // Third message
                if (infoText) infoText.text = thirdMessage;
                if (continuePrompt) continuePrompt.text = continuePromptText;
                break;

            case 3: // Fourth message
                if (infoText) infoText.text = fourthMessage;
                if (continuePrompt) continuePrompt.text = startPromptText;
                break;
        }

        Debug.Log($"Tutorial stage {stage} setup complete");
    }

    void ShowInfoWindow()
    {
        infoWindowActive = true;

        // Show info window
        if (infoWindow)
        {
            infoWindow.SetActive(true);
        }

        // Pause the game time (optional - remove if you don't want time to pause)
        Time.timeScale = 0f;

        Debug.Log($"Displaying tutorial stage {currentStage}");
    }

    void HideInfoWindow()
    {
        infoWindowActive = false;

        // Hide info window
        if (infoWindow)
        {
            infoWindow.SetActive(false);
        }

        // Resume game time
        Time.timeScale = 1f;
    }

    void AdvanceToNextStage()
    {
        // Play advance sound
        if (audioSource && advanceSound)
        {
            audioSource.PlayOneShot(advanceSound);
        }

        currentStage++;

        if (currentStage >= 4) // We have 3 stages (0, 1, 2, 3)
        {
            // Tutorial complete - enable player movement
            CompleteTutorial();
        }
        else
        {
            // Setup next stage
            SetupTutorialStage(currentStage);
        }
    }

    void CompleteTutorial()
    {
        tutorialComplete = true;
        HideInfoWindow();
        EnablePlayerMovement();

        Debug.Log("Tutorial complete! Player can now move.");
    }

    void DisablePlayerMovement()
    {
        if (playerController != null)
        {
            playerController.enabled = false;
            Debug.Log("Player movement disabled for tutorial");
        }
        else
        {
            Debug.LogWarning("Player controller not assigned! Player movement control may not work properly.");
        }
    }

    void EnablePlayerMovement()
    {
        if (playerController != null)
        {
            playerController.enabled = true;
            Debug.Log("Player movement enabled");
        }
    }

    // Public method to manually trigger tutorial (if needed)
    public void StartTutorial()
    {
        if (!tutorialComplete)
        {
            DisablePlayerMovement();
            SetupTutorialStage(0);
            ShowInfoWindow();
        }
    }

    // Public method to skip tutorial (for testing)
    public void SkipTutorial()
    {
        tutorialComplete = true;
        currentStage = 4;
        HideInfoWindow();
        EnablePlayerMovement();
        Debug.Log("Tutorial skipped!");
    }

    // Public method to check if tutorial is complete (other scripts can use this)
    public bool IsTutorialComplete()
    {
        return tutorialComplete;
    }

    void OnDestroy()
    {
        // Make sure time scale is reset when this object is destroyed
        Time.timeScale = 1f;
    }
}