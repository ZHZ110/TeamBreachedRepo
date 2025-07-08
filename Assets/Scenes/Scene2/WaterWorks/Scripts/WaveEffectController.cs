using UnityEngine;

public class WaveEffectController : MonoBehaviour
{
    [Header("Wave Visual Effects")]
    public Transform waterPlane;
    public float waveHeight = 2f;
    public float wavePushDistance = 3f;
    public AnimationCurve waveHeightCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Water Level Animation")]
    public bool animateWaterLevel = false; // Disabled since Water_Settings_Waves handles this
    public float waterLevelRange = 1f;

    [Header("Water Movement")]
    public bool moveWaterPlane = false; // Disabled to avoid conflicts with Water_Settings_Waves
    public float moveSpeed = 2f;

    private WaveSystem waveSystem;
    private Vector3 originalWaterPosition;
    private Vector3 originalWaterScale;

    void Start()
    {
        waveSystem = FindObjectOfType<WaveSystem>();

        if (waterPlane != null)
        {
            originalWaterPosition = waterPlane.position;
            originalWaterScale = waterPlane.localScale;
        }
    }

    void Update()
    {
        if (waveSystem == null || waterPlane == null) return;

        AnimateWaterEffects();
    }

    void AnimateWaterEffects()
    {
        Vector3 waterCenter = waterPlane.position;
        bool isIncoming = waveSystem.IsWaveIncoming(waterCenter);
        float waveStrength = waveSystem.GetWaveStrength(waterCenter);

        // Animate water level (Y position)
        if (animateWaterLevel)
        {
            float heightOffset = 0f;

            if (isIncoming)
            {
                // Wave coming in - water level rises
                heightOffset = waveHeightCurve.Evaluate(waveStrength) * waterLevelRange;
            }
            else
            {
                // Wave going out - water level drops
                heightOffset = -waveHeightCurve.Evaluate(waveStrength) * waterLevelRange * 0.5f;
            }

            Vector3 newPosition = originalWaterPosition;
            newPosition.y += heightOffset;
            waterPlane.position = Vector3.Lerp(waterPlane.position, newPosition, Time.deltaTime * moveSpeed);
        }

        // Move water plane forward/backward
        if (moveWaterPlane)
        {
            Vector3 moveDirection = Vector3.zero;

            if (isIncoming)
            {
                // Move water toward shore
                moveDirection = (waveSystem.shoreline - waterCenter).normalized;
            }
            else
            {
                // Move water toward deep water
                moveDirection = (waveSystem.deepWater - waterCenter).normalized;
            }

            float moveDistance = waveStrength * wavePushDistance;
            Vector3 targetPosition = originalWaterPosition + moveDirection * moveDistance;

            waterPlane.position = Vector3.Lerp(waterPlane.position, targetPosition, Time.deltaTime * moveSpeed);
        }

        // Scale effect (optional - makes waves look bigger/smaller)
        float scaleMultiplier = 1f + (waveStrength * 0.2f);
        Vector3 targetScale = originalWaterScale * scaleMultiplier;
        waterPlane.localScale = Vector3.Lerp(waterPlane.localScale, targetScale, Time.deltaTime * moveSpeed);
    }

    // Reset water to original position
    public void ResetWater()
    {
        if (waterPlane != null)
        {
            waterPlane.position = originalWaterPosition;
            waterPlane.localScale = originalWaterScale;
        }
    }

    void OnDrawGizmos()
    {
        if (waterPlane != null)
        {
            // Draw original position
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(originalWaterPosition, Vector3.one);

            // Draw wave range
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(originalWaterPosition, wavePushDistance);
        }
    }
}