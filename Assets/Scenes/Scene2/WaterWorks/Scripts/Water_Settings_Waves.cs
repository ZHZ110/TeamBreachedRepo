using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class Water_Settings_Waves : MonoBehaviour
{
    Material waterVolume;
    Material waterMaterial;

    [Header("Beach Wave Animation")]
    public bool useBeachWaves = true;
    public float waveIntensity = 1.5f;
    public float baseDisplacement = 0.5f;

    [Header("Visual Effects")]
    public bool animateUVMovement = true;
    public float uvScrollSpeed = 1f;
    public bool animateWaveColor = true;
    public Color advancingWaveColor = new Color(0.2f, 0.4f, 0.8f, 1f); // Darker blue
    public Color retreatingWaveColor = new Color(0.4f, 0.7f, 1f, 1f);   // Lighter blue

    private BeachWaveController beachWaves;
    private Vector2 uvOffset = Vector2.zero;

    void Start()
    {
        if (useBeachWaves)
        {
            beachWaves = FindObjectOfType<BeachWaveController>();
        }
    }

    void Update()
    {
        if (waterVolume == null)
        {
            waterVolume = (Material)Resources.Load("Water_Volume");
        }

        if (waterMaterial == null)
        {
            waterMaterial = GetComponent<MeshRenderer>().sharedMaterial;
        }

        float displacementAmount = baseDisplacement;

        if (useBeachWaves && beachWaves != null)
        {
            bool isAdvancing = beachWaves.IsWaveAdvancing();
            float waveStrength = beachWaves.GetWaveStrength();

            // Animate displacement based on beach wave state
            if (isAdvancing)
            {
                // Wave advancing toward shore - water builds up
                displacementAmount = baseDisplacement + (waveStrength * waveIntensity);
            }
            else
            {
                // Wave retreating to sea - water level drops
                displacementAmount = baseDisplacement - (waveStrength * waveIntensity * 0.6f);
            }

            // Ensure minimum displacement
            displacementAmount = Mathf.Max(0.1f, displacementAmount);

            // Update visual effects
            if (waterMaterial != null)
            {
                // Set displacement
                waterMaterial.SetFloat("_Displacement_Amount", displacementAmount);

                // Animate UV scrolling to show wave direction
                if (animateUVMovement)
                {
                    float scrollDirection = isAdvancing ? 1f : -1f; // Positive = toward shore, Negative = toward sea
                    float scrollSpeed = uvScrollSpeed * waveStrength * scrollDirection;

                    uvOffset.x += Time.deltaTime * scrollSpeed;

                    // Apply UV offset to material
                    if (waterMaterial.HasProperty("_MainTex"))
                        waterMaterial.SetTextureOffset("_MainTex", uvOffset);
                    if (waterMaterial.HasProperty("_BaseMap"))
                        waterMaterial.SetTextureOffset("_BaseMap", uvOffset);
                }

                // Animate wave colors
                if (animateWaveColor)
                {
                    Color targetColor = isAdvancing ? advancingWaveColor : retreatingWaveColor;

                    if (waterMaterial.HasProperty("_Color"))
                        waterMaterial.SetColor("_Color", Color.Lerp(waterMaterial.GetColor("_Color"), targetColor, Time.deltaTime * 2f));
                    if (waterMaterial.HasProperty("_BaseColor"))
                        waterMaterial.SetColor("_BaseColor", Color.Lerp(waterMaterial.GetColor("_BaseColor"), targetColor, Time.deltaTime * 2f));
                }
            }
        }
        else
        {
            // Default behavior when not using beach waves
            if (waterMaterial != null && waterMaterial.HasProperty("_Displacement_Amount"))
            {
                displacementAmount = waterMaterial.GetFloat("_Displacement_Amount");
            }
        }

        // Update water volume position
        if (waterVolume != null)
        {
            Vector4 position = new Vector4(
                0,
                (waterVolume.GetVector("bounds").y / -2) + transform.position.y + (displacementAmount / 3),
                0,
                0
            );
            waterVolume.SetVector("pos", position);
        }
    }

    // Reset UV offsets
    public void ResetUVOffsets()
    {
        uvOffset = Vector2.zero;

        if (waterMaterial != null)
        {
            if (waterMaterial.HasProperty("_MainTex"))
                waterMaterial.SetTextureOffset("_MainTex", Vector2.zero);
            if (waterMaterial.HasProperty("_BaseMap"))
                waterMaterial.SetTextureOffset("_BaseMap", Vector2.zero);
        }
    }

    // Visual debugging
    void OnDrawGizmos()
    {
        if (useBeachWaves && beachWaves != null && Application.isPlaying)
        {
            Vector3 center = transform.position;
            bool isAdvancing = beachWaves.IsWaveAdvancing();
            float strength = beachWaves.GetWaveStrength();

            // Draw wave direction
            Gizmos.color = isAdvancing ? Color.red : Color.cyan;
            Vector3 direction = isAdvancing ?
                (beachWaves.GetShorelinePosition() - center).normalized :
                (beachWaves.GetDeepWaterPosition() - center).normalized;

            Gizmos.DrawLine(center, center + direction * strength * 3f);

#if UNITY_EDITOR
            string waveState = isAdvancing ? "ADVANCING TO SHORE" : "RETREATING TO SEA";
            UnityEditor.Handles.Label(center + Vector3.up * 2f, waveState);
#endif
        }
    }
}