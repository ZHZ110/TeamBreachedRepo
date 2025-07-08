using UnityEngine;

public class BeachWaveController : MonoBehaviour
{
    [Header("Beach Setup")]
    [Tooltip("Leave at 0,0,0 to auto-calculate based on water plane position")]
    public Vector3 shorelinePosition = Vector3.zero; // Will be auto-set if left at zero
    [Tooltip("Leave at 0,0,0 to auto-calculate based on water plane position")]
    public Vector3 deepWaterPosition = Vector3.zero; // Will be auto-set if left at zero
    public float beachWidth = 20f; // How wide the beach area is
    public float beachLength = 5f; // How far shore/deep water are from the water plane
    public float beachSurfaceY = 1f; // Y position of the beach surface (top of your 2x1x2 plane)

    [Header("Wave Properties")]
    public float waveSpeed = 0.5f; // Slower waves for better timing
    public float waveHeight = 0.2f; // Keep this small for realistic waves
    public float waveLength = 8f; // Distance between wave crests
    public float shorelineReach = 3f; // How far waves reach across the beach

    [Header("Water Movement")]
    public Transform waterPlane;
    public float maxWaterAdvance = 2f; // Reduced for more realistic beach waves
    public float waterReturnSpeed = 4f; // Increased for faster, more visible movement

    private Vector3 originalWaterPosition;
    private float wavePhase;

    void Start()
    {
        if (waterPlane != null)
        {
            originalWaterPosition = waterPlane.position;

            // DON'T change the water's Y position - keep it where it is

            // Auto-calculate positions if not set
            if (shorelinePosition == Vector3.zero)
            {
                shorelinePosition = new Vector3(originalWaterPosition.x + beachLength, originalWaterPosition.y, originalWaterPosition.z);
            }

            if (deepWaterPosition == Vector3.zero)
            {
                deepWaterPosition = new Vector3(originalWaterPosition.x - beachLength, originalWaterPosition.y, originalWaterPosition.z);
            }

            //Debug.Log($"Water at: {originalWaterPosition}");
            //Debug.Log($"Shoreline at: {shorelinePosition}");
            //Debug.Log($"Deep water at: {deepWaterPosition}");
        }
    }

    void Update()
    {
        wavePhase += Time.deltaTime * waveSpeed;

        if (waterPlane != null)
        {
            AnimateBeachWaves();
        }
    }

    void AnimateBeachWaves()
    {
        // Calculate wave state
        float waveValue = Mathf.Sin(wavePhase);
        bool isAdvancing = waveValue > 0; // Wave moving toward shore
        float waveIntensity = Mathf.Abs(waveValue);

        Vector3 targetPosition = originalWaterPosition;

        if (isAdvancing)
        {
            // Wave advances UP the beach - move in +X direction ONLY
            float advanceDistance = waveIntensity * maxWaterAdvance;
            targetPosition = new Vector3(
                originalWaterPosition.x + advanceDistance, // Move UP the beach (+X)
                originalWaterPosition.y, // Keep EXACT same Y as original
                originalWaterPosition.z  // Keep same Z
            );
        }
        else
        {
            // Wave retreats DOWN the beach - move in -X direction ONLY
            float retreatDistance = waveIntensity * (maxWaterAdvance * 0.8f);
            targetPosition = new Vector3(
                originalWaterPosition.x - retreatDistance, // Move DOWN the beach (-X)
                originalWaterPosition.y, // Keep EXACT same Y as original
                originalWaterPosition.z  // Keep same Z
            );
        }

        // Debug the movement
        //Debug.Log($"Wave {(isAdvancing ? "UP BEACH (+X)" : "DOWN BEACH (-X)")} - Target: {targetPosition}, Current: {waterPlane.position}");
        //Debug.Log($"Original water position: {originalWaterPosition}");

        // Smooth movement with better lerp
        float lerpSpeed = waterReturnSpeed * Time.deltaTime;
        waterPlane.position = Vector3.Lerp(waterPlane.position, targetPosition, lerpSpeed);

        // If water gets too far from target, snap it closer (prevents getting stuck)
        float distance = Vector3.Distance(waterPlane.position, targetPosition);
        if (distance > maxWaterAdvance * 2f)
        {
            waterPlane.position = Vector3.Lerp(originalWaterPosition, targetPosition, 0.5f);
            //Debug.Log("Snapped water closer to target");
        }
    }

    // For whale movement - call this from WhaleController
    public float GetBeachWaveMultiplier(Vector3 whalePosition)
    {
        float waveValue = Mathf.Sin(wavePhase);
        bool isAdvancing = waveValue > 0;

        // Distance from shore affects wave strength
        float distanceFromShore = Vector3.Distance(whalePosition, shorelinePosition);
        float beachEffect = 1f - Mathf.Clamp01(distanceFromShore / beachWidth);

        if (isAdvancing)
        {
            // Wave pushing toward shore - harder to swim away from shore
            return 0.3f + (beachEffect * 0.2f); // Very hard to move when close to shore
        }
        else
        {
            // Wave retreating to sea - easier to follow the water out
            return 1.5f + (beachEffect * 1f); // Much easier when close to shore
        }
    }

    public bool IsWaveAdvancing()
    {
        return Mathf.Sin(wavePhase) > 0;
    }

    public float GetWaveStrength()
    {
        return Mathf.Abs(Mathf.Sin(wavePhase));
    }

    // Public access to positions for other scripts
    public Vector3 GetShorelinePosition()
    {
        return shorelinePosition;
    }

    public Vector3 GetDeepWaterPosition()
    {
        return deepWaterPosition;
    }

    // Visual debugging
    void OnDrawGizmos()
    {
        if (shorelinePosition != Vector3.zero && deepWaterPosition != Vector3.zero)
        {
            // Draw shoreline
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(shorelinePosition + Vector3.left * 10f, shorelinePosition + Vector3.right * 10f);

            // Draw deep water line
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(deepWaterPosition + Vector3.left * 10f, deepWaterPosition + Vector3.right * 10f);

            // Draw beach area
            Gizmos.color = Color.green;
            Vector3 beachCenter = Vector3.Lerp(shorelinePosition, deepWaterPosition, 0.3f);
            Gizmos.DrawWireCube(beachCenter, new Vector3(20f, 1f, beachWidth));

            if (Application.isPlaying)
            {
                // Show current wave state
                bool advancing = IsWaveAdvancing();
                float strength = GetWaveStrength();

                Gizmos.color = advancing ? Color.red : Color.cyan;
                Vector3 direction = advancing ?
                    (shorelinePosition - transform.position).normalized :
                    (deepWaterPosition - transform.position).normalized;

                Gizmos.DrawLine(transform.position, transform.position + direction * strength * 5f);

#if UNITY_EDITOR
                string waveState = advancing ? "WAVE ADVANCING TO SHORE" : "WAVE RETREATING TO SEA";
                UnityEditor.Handles.Label(transform.position + Vector3.up * 3f, waveState);
#endif
            }
        }
    }
}