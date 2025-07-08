using UnityEngine;

public class WaveFoamController : MonoBehaviour
{
    [Header("Foam Particles")]
    public ParticleSystem foamParticles;
    public Material foamMaterial; // Assign your custom foam material here
    public int foamBurstAmount = 120; // More particles for larger area
    public float foamLifetime = 3f; // Longer lasting foam

    [Header("Foam Positioning")]
    public Transform waterPlane;
    public float foamOffset = 0.1f; // Height above water
    public float foamWidth = 25f; // Much wider foam spread across beach (was 15f)

    [Header("Foam Timing")]
    public float foamTriggerThreshold = 0.6f; // Wave strength needed to trigger foam (0-1)
    public float foamTriggerDelay = 0.2f; // Delay between foam bursts
    public bool foamOnDirectionChange = true; // Create foam when wave direction changes
    public bool foamOnPeakStrength = true; // Create foam at peak wave strength

    private BeachWaveController beachWaves;
    private bool wasAdvancing = false;
    private float lastFoamTime = 0f;

    void Start()
    {
        beachWaves = FindObjectOfType<BeachWaveController>();

        if (foamParticles == null)
        {
            CreateFoamParticleSystem();
        }

        SetupFoamParticles();
    }

    void CreateFoamParticleSystem()
    {
        GameObject foamGO = new GameObject("Wave Foam");
        foamGO.transform.SetParent(transform);
        foamParticles = foamGO.AddComponent<ParticleSystem>();
    }

    void SetupFoamParticles()
    {
        if (foamParticles == null) return;

        var main = foamParticles.main;
        main.startLifetime = foamLifetime;
        main.startSpeed = 1f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.8f); // Keep bubble sizes the same
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 1f, 1f, 0.7f),    // Semi-transparent white
            new Color(0.9f, 0.95f, 1f, 0.5f) // Slightly blue, more transparent
        );
        main.maxParticles = 800; // More particles to fill the larger area
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        // Don't modify materials - let user set them manually
        // (Material setup removed to preserve user's custom materials)

        // Don't modify modules that cause editor GUI errors
        // Set up Noise module manually in the inspector if needed

        var emission = foamParticles.emission;
        emission.enabled = false; // We'll emit manually

        var shape = foamParticles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box; // Spawn area is rectangular (good for beach foam line)
        shape.scale = new Vector3(2f, 0.2f, foamWidth); // Z-scale will be 25 (much wider spawn area)

        var velocityOverLifetime = foamParticles.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-1f, 1f); // Movement up/down beach
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(0.5f, 3f); // Upward movement
        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(-3f, 3f); // Wider spread across beach (increased from -2f, 2f)

        var sizeOverLifetime = foamParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.5f);   // Start small
        sizeCurve.AddKey(0.3f, 1f);   // Grow bigger
        sizeCurve.AddKey(1f, 0f);     // Fade to nothing
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Add color over lifetime for realistic foam fade
        var colorOverLifetime = foamParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0.0f),
                new GradientColorKey(new Color(0.8f, 0.9f, 1f), 0.5f),
                new GradientColorKey(new Color(0.6f, 0.8f, 1f, 0f), 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(0.8f, 0.5f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        colorOverLifetime.color = gradient;
    }

    void Update()
    {
        if (beachWaves == null || waterPlane == null) return;

        bool isAdvancing = beachWaves.IsWaveAdvancing();
        float waveStrength = beachWaves.GetWaveStrength();
        bool shouldCreateFoam = false;

        // Check if enough time has passed since last foam
        bool cooldownReady = (Time.time - lastFoamTime) > foamTriggerDelay;

        // Create foam when wave direction changes
        if (foamOnDirectionChange && (isAdvancing != wasAdvancing) && cooldownReady)
        {
            shouldCreateFoam = true;
        }

        // Create foam at peak wave strength
        if (foamOnPeakStrength && waveStrength > foamTriggerThreshold && cooldownReady)
        {
            shouldCreateFoam = true;
        }

        if (shouldCreateFoam)
        {
            CreateFoam(waveStrength);
            lastFoamTime = Time.time;
        }

        // Position foam system at the leading edge of the water
        Vector3 foamPosition = waterPlane.position;

        if (isAdvancing)
        {
            // Foam at the front edge when advancing up beach (+X direction)
            foamPosition.x += 1f; // Slightly ahead of water up the beach
        }
        else
        {
            // Foam at the retreating edge when going down beach (-X direction)
            foamPosition.x -= 0.5f; // Slightly behind water down the beach
        }

        foamPosition.y += foamOffset;
        foamParticles.transform.position = foamPosition;

        wasAdvancing = isAdvancing;
    }

    void CreateFoam(float intensity)
    {
        if (foamParticles == null) return;

        int particleCount = Mathf.RoundToInt(foamBurstAmount * intensity);
        foamParticles.Emit(particleCount);
    }
}