using UnityEngine;

public class WaveEdgeDistortion : MonoBehaviour
{
    [Header("Edge Distortion")]
    public float noiseScale = 2f;
    public float noiseStrength = 0.5f;
    public float noiseSpeed = 1f;

    [Header("Water Plane")]
    public Transform waterPlane;
    public int edgeResolution = 20; // Number of points along the edge

    private BeachWaveController beachWaves;
    private Vector3 originalScale;
    private float timeOffset;

    void Start()
    {
        beachWaves = FindObjectOfType<BeachWaveController>();

        if (waterPlane != null)
        {
            originalScale = waterPlane.localScale;
        }

        timeOffset = Random.Range(0f, 100f); // Random offset for each wave
    }

    void Update()
    {
        if (beachWaves == null || waterPlane == null) return;

        float waveStrength = beachWaves.GetWaveStrength();
        bool isAdvancing = beachWaves.IsWaveAdvancing();

        // Create irregular edges by slightly scaling and rotating
        CreateIrregularEdge(waveStrength, isAdvancing);
    }

    void CreateIrregularEdge(float waveStrength, bool isAdvancing)
    {
        float time = Time.time * noiseSpeed + timeOffset;

        // Create noise-based scale variation
        float noiseX = Mathf.PerlinNoise(time * noiseScale, 0f) - 0.5f;
        float noiseZ = Mathf.PerlinNoise(0f, time * noiseScale) - 0.5f;

        Vector3 scaleVariation = new Vector3(
            noiseX * noiseStrength * waveStrength,
            0f,
            noiseZ * noiseStrength * waveStrength
        );

        waterPlane.localScale = originalScale + scaleVariation;

        // Add slight rotation for more natural movement
        float rotationNoise = Mathf.PerlinNoise(time * 0.5f, timeOffset) - 0.5f;
        float rotationAmount = rotationNoise * 2f * waveStrength; // Small rotation

        waterPlane.rotation = Quaternion.Euler(0f, rotationAmount, 0f);
    }
}