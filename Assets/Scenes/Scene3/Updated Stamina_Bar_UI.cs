using UnityEngine;
using UnityEngine.UI;

public class Updated_Stamina_Bar_UI : MonoBehaviour
{
    [SerializeField] private WhaleStaminaSystem staminaSystem;
    [SerializeField] private Image barImage;

    private void Start()
    {
        // Find the stamina system if not assigned
        if (staminaSystem == null)
        {
            staminaSystem = FindObjectOfType<WhaleStaminaSystem>();
        }

        // Check if barImage is assigned
        if (barImage == null)
        {
            Debug.LogError("Bar Image is not assigned in Stamina_Bar_UI!");
            return;
        }

        if (staminaSystem != null)
        {
            staminaSystem.OnStaminaChanged += StaminaSystem_OnStaminaChanged;
        }
        else
        {
            Debug.LogError("WhaleStaminaSystem not found!");
        }

        barImage.fillAmount = 1.0f;
    }

    private void StaminaSystem_OnStaminaChanged(object sender, WhaleStaminaSystem.OnStaminaChangedEventArgs e)
    {
        if (barImage != null)
        {
            barImage.fillAmount = e.staminaNormalized;
        }
    }

    private void OnDestroy()
    {
        if (staminaSystem != null)
        {
            staminaSystem.OnStaminaChanged -= StaminaSystem_OnStaminaChanged;
        }
    }
}