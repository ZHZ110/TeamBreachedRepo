using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InfoWindowSystem : MonoBehaviour
{
    [Header("UI References")]
    public GameObject infoWindow;
    public TextMeshProUGUI infoText;
    public TextMeshProUGUI continuePrompt;

    [Header("Info Steps")]
    [TextArea(3, 5)]
    public string[] infoTexts = {
        "Oh no! I'm stuck on a beach. I must have been pushed up here by the storm. How can I get back into the water?",
        "Don't panic. Maybe I can use the same movements the matriarch taught me about breaching.",
        "Let's press W to wiggle back and forth until I get back into the water.",
        "I feel like I can move further if I wiggle when the waves are going out. Let me try timing it right!"
    };
    [TextArea(3, 5)]
    public string[] promptTexts = {
        "Press SPACE to continue",
        "Press SPACE to continue",
        "Press SPACE to continue",
        "Press SPACE to start playing"
    };

    [Header("Settings")]
    public KeyCode continueKey = KeyCode.Space;
    public bool showOnStart = true;

    private bool infoWindowActive = false;
    private int currentStep = 0;

    void Start()
    {
        // Hide info window initially
        if (infoWindow) infoWindow.SetActive(false);

        // Show info window if enabled
        if (showOnStart)
        {
            ShowInfoWindow();
        }
    }

    void Update()
    {
        if (infoWindowActive)
        {
            if (Input.GetKeyDown(continueKey))
            {
                NextStep();
            }
        }
    }

    public void ShowInfoWindow()
    {
        if (infoWindowActive) return;

        infoWindowActive = true;

        // Show info window
        if (infoWindow) infoWindow.SetActive(true);

        // Set current step text
        UpdateInfoText();

        Debug.Log($"Info window displayed - Step {currentStep + 1}. Press {continueKey} to continue...");
    }

    public void NextStep()
    {
        currentStep++;

        // Check if we've reached the end
        if (currentStep >= infoTexts.Length)
        {
            HideInfoWindow();
            return;
        }

        // Update to next step
        UpdateInfoText();
        Debug.Log($"Advanced to step {currentStep + 1}");
    }

    void UpdateInfoText()
    {
        if (currentStep < infoTexts.Length)
        {
            if (infoText) infoText.text = infoTexts[currentStep];

            if (continuePrompt && currentStep < promptTexts.Length)
            {
                continuePrompt.text = promptTexts[currentStep];
            }
        }
    }

    public void HideInfoWindow()
    {
        infoWindowActive = false;
        if (infoWindow) infoWindow.SetActive(false);

        Debug.Log("Info window sequence completed!");

        // You can add any completion logic here
        OnInfoSequenceComplete();
    }

    // Override this method or add UnityEvents to handle what happens after info sequence
    protected virtual void OnInfoSequenceComplete()
    {
        //Debug.Log("Info sequence finished - add your custom logic here!");
    }

    // Public method to restart the sequence
    public void RestartSequence()
    {
        currentStep = 0;
        ShowInfoWindow();
    }

    // Public method to jump to a specific step
    public void GoToStep(int stepIndex)
    {
        if (stepIndex >= 0 && stepIndex < infoTexts.Length)
        {
            currentStep = stepIndex;
            UpdateInfoText();
        }
    }

    // Public method to trigger the info window from other scripts
    public void TriggerInfoWindow()
    {
        ShowInfoWindow();
    }
}