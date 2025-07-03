using UnityEngine;

public class EcholocationWave : MonoBehaviour
{
    [Header("Wave Settings")]
    public float startSize = 0.5f;
    public float maxSize = 20f;
    public float speed = 10f;
    public float duration = 2f;
    public Color waveColor = Color.green;

    [Header("Arc Settings")]
    public float arcAngle = 60f; // Smaller quarter circle arc
    public int segments = 20; // Smoothness of the arc

    private LineRenderer lineRenderer;
    private float currentSize;
    private float timer;
    private Vector3 playerForward;

    public void Initialize(float startSize, float maxSize, float speed, float duration, Color color)
    {
        this.startSize = startSize;
        this.maxSize = maxSize;
        this.speed = speed;
        this.duration = duration;
        this.waveColor = color;

        currentSize = startSize;
        timer = 0f;

        // Get the player's forward direction when the wave is created
        Camera playerCamera = Camera.main;
        if (playerCamera == null)
            playerCamera = FindFirstObjectByType<Camera>();

        if (playerCamera != null)
        {
            playerForward = playerCamera.transform.forward;
            playerForward.y = 0; // Keep it horizontal
            playerForward.Normalize();
        }
        else
        {
            playerForward = Vector3.forward; // Fallback
        }

        SetupLineRenderer();
        UpdateWaveShape();
    }

    void SetupLineRenderer()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();

        // Configure the LineRenderer
        lineRenderer.material = CreateWaveMaterial();
        lineRenderer.startWidth = 0.15f;
        lineRenderer.endWidth = 0.15f;
        lineRenderer.positionCount = segments + 1;
        lineRenderer.useWorldSpace = true;
        lineRenderer.sortingOrder = 10;

        // Disable shadows for better performance
        lineRenderer.receiveShadows = false;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    Material CreateWaveMaterial()
    {
        // Create a simple unlit material with less transparency
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = waveColor;

        // Make it less transparent and more visible
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;

        return mat;
    }

    void Update()
    {
        timer += Time.deltaTime;

        // Expand the wave outward
        currentSize = Mathf.Lerp(startSize, maxSize, timer / duration);

        // Fade out more gradually for better opacity
        float alpha = 1f - (timer / duration);
        alpha = Mathf.Clamp01(alpha * 1.5f); // Make it stay more opaque longer

        Color currentColor = waveColor;
        currentColor.a = alpha * 0.8f; // Base opacity of 80%

        if (lineRenderer != null && lineRenderer.material != null)
        {
            lineRenderer.material.color = currentColor;
            UpdateWaveShape();
        }

        // Destroy when animation is complete
        if (timer >= duration)
        {
            Destroy(gameObject);
        }
    }

    void UpdateWaveShape()
    {
        if (lineRenderer == null) return;

        Vector3[] positions = new Vector3[segments + 1];

        // Calculate the starting angle based on player's forward direction
        float forwardAngle = Mathf.Atan2(playerForward.x, playerForward.z) * Mathf.Rad2Deg;
        float startAngle = forwardAngle - (arcAngle / 2f);

        // Create arc points
        for (int i = 0; i <= segments; i++)
        {
            float angle = startAngle + (arcAngle * i / segments);
            float radians = angle * Mathf.Deg2Rad;

            // Calculate position on the arc
            Vector3 direction = new Vector3(Mathf.Sin(radians), 0, Mathf.Cos(radians));
            Vector3 position = transform.position + direction * currentSize;

            // Keep the wave at a higher level (near whale's mouth)
            position.y = transform.position.y;

            positions[i] = position;
        }

        lineRenderer.SetPositions(positions);
    }

    void OnDestroy()
    {
        if (lineRenderer != null && lineRenderer.material != null)
        {
            DestroyImmediate(lineRenderer.material);
        }
    }
}