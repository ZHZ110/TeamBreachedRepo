using System;
using UnityEngine;

public class WhaleStaminaSystem : MonoBehaviour
{
    [Header("Stamina Settings")]
    [SerializeField] private int maxStamina = 1000;
    [SerializeField] private int staminaRegenRate = 2; // per second when resting

    [Header("Temperature Settings")]
    [SerializeField] private float maxTemperature = 30f;
    [SerializeField] private float temperatureRiseRate = 0.6f; // per second
    [SerializeField] private float coolingRate = 1.0f; // per second when using S key

    // Current values
    private int currentStamina;
    private float currentTemperature = 0f;

    // Input states
    private bool isUsingStamina = false; // W key equivalent
    private bool isCooling = false; // S key equivalent

    // Events for UI updates
    public event EventHandler<OnStaminaChangedEventArgs> OnStaminaChanged;
    public event EventHandler<OnTemperatureChangedEventArgs> OnTemperatureChanged;

    public class OnStaminaChangedEventArgs : EventArgs
    {
        public float staminaNormalized;
    }

    public class OnTemperatureChangedEventArgs : EventArgs
    {
        public float temperatureNormalized;
    }

    private void Start()
    {
        currentStamina = maxStamina;
        currentTemperature = 0f;
    }

    private void Update()
    {
        HandleStamina();
        HandleTemperature();
    }

    private void HandleStamina()
    {
        if (isUsingStamina && currentStamina > 0)
        {
            // Drain stamina
            currentStamina = Mathf.Max(0, currentStamina - Mathf.RoundToInt(60f * Time.deltaTime)); // 60 per second
        }
        else if (!isUsingStamina && currentStamina < maxStamina)
        {
            // Regenerate stamina when not being used
            currentStamina = Mathf.Min(maxStamina, currentStamina + Mathf.RoundToInt(staminaRegenRate * Time.deltaTime));
        }

        // Fire stamina changed event
        OnStaminaChanged?.Invoke(this, new OnStaminaChangedEventArgs
        {
            staminaNormalized = (float)currentStamina / maxStamina
        });
    }

    private void HandleTemperature()
    {
        if (isCooling && currentTemperature > 0)
        {
            // Cool down when using S key equivalent
            currentTemperature = Mathf.Max(0f, currentTemperature - coolingRate * Time.deltaTime);
        }
        else
        {
            // Temperature always rises naturally
            currentTemperature = Mathf.Min(maxTemperature, currentTemperature + temperatureRiseRate * Time.deltaTime);
        }

        // Fire temperature changed event
        OnTemperatureChanged?.Invoke(this, new OnTemperatureChangedEventArgs
        {
            temperatureNormalized = currentTemperature / maxTemperature
        });
    }

    // Public methods for other scripts to call
    public void SetStaminaUsage(bool isUsing)
    {
        isUsingStamina = isUsing;
    }

    public void SetCooling(bool cooling)
    {
        isCooling = cooling;
    }

    public bool HasStamina()
    {
        return currentStamina > 0;
    }

    public bool IsOverheating()
    {
        return currentTemperature >= maxTemperature;
    }

    // Getters
    public int GetStamina() => currentStamina;
    public float GetTemperature() => currentTemperature;
    public float GetStaminaNormalized() => (float)currentStamina / maxStamina;
    public float GetTemperatureNormalized() => currentTemperature / maxTemperature;
}