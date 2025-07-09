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
    public MonoBehaviour playerController;
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
    public AudioSource tutorialAudioSource; // Dedicated AudioSource for tutorial
    public AudioClip advanceSound;
    [Range(0f, 1f)]
    public float tutorialSoundVolume = 1f;

    private bool infoWindowActive = false;
    private int currentStage = 0;
    private bool tutorialComplete = false;

    void Start()
    {
        // Create dedicated AudioSource for tutorial sounds
        if (tutorialAudioSource == null)
        {
            GameObject tutorialAudioGO = new GameObject("TutorialAudio");
            tutorialAudioGO.transform.SetParent(transform);
            tutorialAudioGO.transform.localPosition = Vector3.zero;
            tutorialAudioSource = tutorialAudioGO.AddComponent<AudioSource>();
        }

        // Setup tutorial AudioSource
        tutorialAudioSource.playOnAwake = false;
        tutorialAudioSource.spatialBlend = 0f; // 2D sound for UI

        //Debug.Log($"Tutorial audio setup complete. Advance sound: {advanceSound != null}");

        DisablePlayerMovement();
        SetupTutorialStage(0);
        ShowInfoWindow();
    }

    void Update()
    {
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
            case 0:
                if (infoText) infoText.text = firstMessage;
                if (continuePrompt) continuePrompt.text = continuePromptText;
                break;
            case 1:
                if (infoText) infoText.text = secondMessage;
                if (continuePrompt) continuePrompt.text = continuePromptText;
                break;
            case 2:
                if (infoText) infoText.text = thirdMessage;
                if (continuePrompt) continuePrompt.text = continuePromptText;
                break;
            case 3:
                if (infoText) infoText.text = fourthMessage;
                if (continuePrompt) continuePrompt.text = startPromptText;
                break;
        }

        //Debug.Log($"Tutorial stage {stage} setup complete");
    }

    void ShowInfoWindow()
    {
        infoWindowActive = true;

        if (infoWindow)
        {
            infoWindow.SetActive(true);
        }

        Time.timeScale = 0f;
        //Debug.Log($"Displaying tutorial stage {currentStage}");
    }

    void HideInfoWindow()
    {
        infoWindowActive = false;

        if (infoWindow)
        {
            infoWindow.SetActive(false);
        }

        Time.timeScale = 1f;
    }

    void AdvanceToNextStage()
    {
        // Play advance sound
        if (tutorialAudioSource != null && advanceSound != null)
        {
            tutorialAudioSource.clip = advanceSound;
            tutorialAudioSource.volume = tutorialSoundVolume;
            tutorialAudioSource.Play();
            //Debug.Log("Tutorial advance sound played");
        }
        else
        {
            //Debug.LogWarning($"Cannot play tutorial sound. AudioSource: {tutorialAudioSource != null}, Clip: {advanceSound != null}");
        }

        currentStage++;

        if (currentStage >= 4)
        {
            CompleteTutorial();
        }
        else
        {
            SetupTutorialStage(currentStage);
        }
    }

    void CompleteTutorial()
    {
        tutorialComplete = true;
        HideInfoWindow();
        EnablePlayerMovement();
        //Debug.Log("Tutorial complete! Player can now move.");
    }

    void DisablePlayerMovement()
    {
        if (playerController != null)
        {
            playerController.enabled = false;
            //Debug.Log("Player movement disabled for tutorial");
        }
        else
        {
            //Debug.LogWarning("Player controller not assigned!");
        }
    }

    void EnablePlayerMovement()
    {
        if (playerController != null)
        {
            playerController.enabled = true;
            //Debug.Log("Player movement enabled");
        }
    }

    public void StartTutorial()
    {
        if (!tutorialComplete)
        {
            DisablePlayerMovement();
            SetupTutorialStage(0);
            ShowInfoWindow();
        }
    }

    public void SkipTutorial()
    {
        tutorialComplete = true;
        currentStage = 4;
        HideInfoWindow();
        EnablePlayerMovement();
        //Debug.Log("Tutorial skipped!");
    }

    public bool IsTutorialComplete()
    {
        return tutorialComplete;
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}