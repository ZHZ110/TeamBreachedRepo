using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Timeline;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 1.0f;
    [SerializeField] private Wave wave1;
    [SerializeField] private Wave wave2;
    [SerializeField] private Wave wave3;
    [SerializeField] private BeachWaveController beachWaveController;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip wiggleSound;
    [SerializeField] private AudioClip splashSound; // Optional: for S key splashing

    public enum SignalType
    {
        Wave_Stop,
        Echolocation,
        Orca_approached,
        Wave_PullOrca,
    }

    public event EventHandler<OnStaminaChangedEventArgs> onStaminaChanged;
    public class OnStaminaChangedEventArgs : EventArgs
    {
        public float staminaNormalized;
    }

    private bool s_pressed;
    private bool w_pressed;
    private bool w_pressed_last_frame = false; // Track previous frame state
    private int stamina = 1000;
    private int STAMINA_MAX = 1000;
    private float temperature = 0;
    private float TEMPERATURE_MAX = 30;
    private bool taken_by_wave = false;
    private Vector3 wave_location;
    private bool isInWater = false;
    private bool hasWon = false;

    private void Start()
    {
        Transform childTransform = transform.Find("Capsule");
        childTransform.localPosition = Vector3.zero;
        childTransform.localRotation = Quaternion.identity;
        childTransform.localScale = Vector3.one;

        // Get AudioSource if not assigned
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();

            // If still null, try to find one on child objects
            if (audioSource == null)
            {
                audioSource = GetComponentInChildren<AudioSource>();
            }

            // If still null, add one
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                Debug.Log("Added AudioSource component to Player");
            }
        }
    }

    private void Update()
    {
        Vector2 inputVector = new Vector2(0, 0);
        float rotateSpeed = 1.0f;
        if (Input.GetKeyDown(KeyCode.A))
        {
            //Debug.Log("Arching!");
            inputVector.x = -1f;
        }
        if (Input.GetKey(KeyCode.W))
        {
            //Debug.Log("Wigging Tail!");
            w_pressed = true;

            // Play wiggle sound when W is first pressed (not held)
            if (!w_pressed_last_frame && audioSource && wiggleSound)
            {
                audioSource.PlayOneShot(wiggleSound);
            }

            Vector3 baseMovement = new Vector3(-2.0f, 0f, 0f) * moveSpeed * Time.deltaTime;

            if (beachWaveController != null)
            {
                float waveMultiplier = beachWaveController.GetBeachWaveMultiplier(transform.position);
                baseMovement *= waveMultiplier;

                // Debug wave effects
                bool isAdvancing = beachWaveController.IsWaveAdvancing();
                float waveStrength = beachWaveController.GetWaveStrength();

                // Check if whale moved at the right time
                string timingFeedback = "Neutral timing.";

                if (!isAdvancing && waveMultiplier > 1.0f)
                {
                    timingFeedback = "PERFECT TIMING! Wave is helping you move!";
                }
                else if (isAdvancing && waveMultiplier < 1.0f)
                {
                    timingFeedback = "BAD TIMING! Wave is working against you.";
                }

                Debug.Log($"Wave {(isAdvancing ? "coming in" : "going out")}, strength: {waveStrength:F2}, multiplier: {waveMultiplier:F2} - {timingFeedback}");
            }

            else
            {
                Debug.Log("No BeachWaveController assigned!");
            }

            transform.position += baseMovement;
            stamina = stamina - 1;
            onStaminaChanged?.Invoke(this, new OnStaminaChangedEventArgs
            {
                staminaNormalized = (float)stamina / STAMINA_MAX
            });
        }
        else
        {
            w_pressed = false;
        }

        // Update last frame state for next frame
        w_pressed_last_frame = w_pressed;

        if (Input.GetKey(KeyCode.S))
        {
            s_pressed = true;

            // Play splash sound when S is first pressed (optional)
            if (Input.GetKeyDown(KeyCode.S) && audioSource && splashSound)
            {
                audioSource.PlayOneShot(splashSound);
            }

            --temperature;
            --stamina;
            onStaminaChanged?.Invoke(this, new OnStaminaChangedEventArgs
            {
                staminaNormalized = (float)stamina / STAMINA_MAX
            });
            //Debug.Log("Splashing water with tail!");
        }
        else
        {
            s_pressed = false;
        }
        Vector3 moveDir = new Vector3(inputVector.x, 0f, inputVector.y);
        if (Input.GetKeyDown(KeyCode.R))
        {
            //Debug.Log("Rotate!");
            Vector3 rotateDir = new Vector3(0.01f, 0f, 0f);
            //transform.forward += new Vector3(0.01f, 0f, 0f);
            transform.forward = Vector3.Slerp(transform.forward, rotateDir, Time.deltaTime * rotateSpeed);
        }

        // Move in world space
        if (!taken_by_wave && moveDir != Vector3.zero)
        {
            transform.position += moveDir * moveSpeed * Time.deltaTime;
        }

        temperature += 0.01f;

        //Debug.Log("Position: " + transform.position);
        //Debug.Log("Forward direction: " + transform.forward);
        //Debug.Log("Capsule position: " + transform.Find("Capsule").position);


        //Debug.Log(transform.position);
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            wave1.SetSignal(SignalType.Echolocation, transform.Find("Capsule").position);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            wave2.SetSignal(SignalType.Echolocation, transform.Find("Capsule").position);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            wave3.SetSignal(SignalType.Echolocation, transform.Find("Capsule").position);
        }
        if (taken_by_wave)
        {
            // might need to modify this part in the future
            transform.position = Vector3.MoveTowards(transform.position,
                wave_location, moveSpeed * Time.deltaTime);
        }
    }

    public bool S_pressed()
    {
        return s_pressed;
    }

    public bool W_pressed()
    {
        return w_pressed;
    }

    public int GetStamina()
    {
        return stamina;
    }

    public float GetTemperature()
    {
        return temperature;
    }

    public void SetTakenByWave(bool _takenByWave, Vector3 _waveLocation)
    {
        taken_by_wave = _takenByWave;
        wave_location = _waveLocation;
    }

    public void OnCapsuleEnteredWater()
    {
        isInWater = true;
        Debug.Log("Capsule entered water!");
        CheckWinCondition();
    }

    public void OnCapsuleInWater()
    {
        if (!hasWon)
        {
            CheckWinCondition();
        }
    }

    public void OnCapsuleExitedWater()
    {
        isInWater = false;
        Debug.Log("Capsule exited water!");
    }

    private void CheckWinCondition()
    {
        if (isInWater && !hasWon)
        {
            PlayerWins();
        }
    }

    private void PlayerWins()
    {
        hasWon = true;
        SceneManager.LoadScene("PersistentUI");
        Debug.Log("Player Wins! Whale is fully submerged!");
    }
}