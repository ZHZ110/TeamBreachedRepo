// WaveVisualizer.cs
using UnityEngine;

public class WaveVisualizer : MonoBehaviour
{
    public Material waterMaterial;
    public float animationSpeed = 1f;

    private WaveSystem waveSystem;

    void Start()
    {
        waveSystem = FindObjectOfType<WaveSystem>();
    }

    void Update()
    {
        if (waveSystem != null)
        {
            // Update shader properties based on wave state
            bool isIncoming = waveSystem.IsWaveIncoming(transform.position);

            // Animate UV offset based on wave direction
            float offset = Time.time * animationSpeed;
            if (!isIncoming) offset = -offset;

            waterMaterial.SetFloat("_UVOffset", offset);
            waterMaterial.SetFloat("_WavePhase", waveSystem.currentWavePhase);
        }
    }
}