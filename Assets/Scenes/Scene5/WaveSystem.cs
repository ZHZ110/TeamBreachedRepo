// WaveSystem.cs
using UnityEngine;

public class WaveSystem : MonoBehaviour
{
    [Header("Wave Properties")]
    public float waveSpeed = 2f;
    public float waveLength = 10f;
    public float waveHeight = 1f;
    public float waveFrequency = 1f;

    [Header("Movement Multipliers")]
    public float incomingWaveMultiplier = 0.3f; // How much movement is reduced during incoming waves
    public float outgoingWaveMultiplier = 2.5f; // How much movement is increased during outgoing waves

    [Header("Flow Settings")]
    public float flowForce = 10f;
    public float maxFlowDistance = 20f; // How far from shore the flow affects

    public Vector3 shoreline = Vector3.zero;
    public Vector3 deepWater = new Vector3(0, 0, -20f);

    public float currentWavePhase; // Made public for WaveVisualizer

    void Update()
    {
        currentWavePhase = Time.time * waveSpeed;
    }

    // Get the movement multiplier based on current wave state
    public float GetMovementMultiplier(Vector3 position)
    {
        float distanceFromShore = Vector3.Distance(position, shoreline);
        float waveValue = Mathf.Sin(currentWavePhase + distanceFromShore * waveFrequency);

        if (waveValue > 0)
        {
            // Wave is coming in - movement is reduced (fighting against the wave)
            return incomingWaveMultiplier;
        }
        else
        {
            // Wave is going out - movement is increased (riding the wave)
            return outgoingWaveMultiplier;
        }
    }

    // Check if wave is currently incoming or outgoing
    public bool IsWaveIncoming(Vector3 position)
    {
        float distanceFromShore = Vector3.Distance(position, shoreline);
        float waveValue = Mathf.Sin(currentWavePhase + distanceFromShore * waveFrequency);
        return waveValue > 0;
    }

    // Get wave strength (0-1) for visual feedback
    public float GetWaveStrength(Vector3 position)
    {
        float distanceFromShore = Vector3.Distance(position, shoreline);
        return Mathf.Abs(Mathf.Sin(currentWavePhase + distanceFromShore * waveFrequency));
    }

    // Get flow direction at a specific world position (for backwards compatibility)
    public Vector3 GetFlowDirection(Vector3 position)
    {
        float distanceFromShore = Vector3.Distance(position, shoreline);

        // Calculate the wave phase at this position
        float waveValue = Mathf.Sin(currentWavePhase + distanceFromShore * waveFrequency);

        // Positive wave = incoming (towards shore)
        // Negative wave = outgoing (towards deep water)
        if (waveValue > 0)
        {
            // Wave is coming in - flow goes towards shore
            return (shoreline - position).normalized;
        }
        else
        {
            // Wave is going out - flow goes towards deep water
            return (deepWater - position).normalized;
        }
    }

    // Get current flow strength (0-1) (for backwards compatibility)
    public float GetFlowStrength(Vector3 position)
    {
        float distanceFromShore = Vector3.Distance(position, shoreline);
        float waveValue = Mathf.Abs(Mathf.Sin(currentWavePhase + distanceFromShore * waveFrequency));

        // Reduce flow strength with distance from shore
        float distanceFactor = 1f - (distanceFromShore / maxFlowDistance);
        return waveValue * Mathf.Clamp01(distanceFactor);
    }

    // Debug visualization
    void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            // Draw flow direction at various points
            for (int x = -10; x <= 10; x += 2)
            {
                for (int z = -10; z <= 10; z += 2)
                {
                    Vector3 pos = new Vector3(x, 0, z);
                    Vector3 flow = GetFlowDirection(pos);
                    float strength = GetFlowStrength(pos);

                    Gizmos.color = IsWaveIncoming(pos) ? Color.blue : Color.red;
                    Gizmos.DrawLine(pos, pos + flow * strength * 2f);
                }
            }
        }
    }
}