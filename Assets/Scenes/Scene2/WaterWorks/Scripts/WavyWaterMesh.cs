using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class WavyWaterMesh : MonoBehaviour
{
    [Header("Mesh Generation")]
    public int meshWidth = 20;
    public int meshHeight = 20;
    public float meshSize = 10f;

    [Header("Wave Animation")]
    public float waveHeight = 0.3f;
    public float waveSpeed = 2f;
    public float waveFrequency = 1f;
    public float edgeWaveIntensity = 1.5f; // Stronger waves at edges

    private Mesh mesh;
    private Vector3[] originalVertices;
    private Vector3[] vertices;
    private BeachWaveController beachWaves;

    void Start()
    {
        beachWaves = FindObjectOfType<BeachWaveController>();
        GenerateWaterMesh();
    }

    void GenerateWaterMesh()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        mesh = new Mesh();

        // Generate vertices
        vertices = new Vector3[(meshWidth + 1) * (meshHeight + 1)];
        Vector2[] uv = new Vector2[vertices.Length];

        for (int i = 0, y = 0; y <= meshHeight; y++)
        {
            for (int x = 0; x <= meshWidth; x++, i++)
            {
                float xPos = (float)x / meshWidth - 0.5f;
                float yPos = (float)y / meshHeight - 0.5f;

                vertices[i] = new Vector3(xPos * meshSize, 0, yPos * meshSize);
                uv[i] = new Vector2((float)x / meshWidth, (float)y / meshHeight);
            }
        }

        originalVertices = new Vector3[vertices.Length];
        vertices.CopyTo(originalVertices, 0);

        // Generate triangles
        int[] triangles = new int[meshWidth * meshHeight * 6];

        for (int ti = 0, vi = 0, y = 0; y < meshHeight; y++, vi++)
        {
            for (int x = 0; x < meshWidth; x++, ti += 6, vi++)
            {
                triangles[ti] = vi;
                triangles[ti + 3] = triangles[ti + 2] = vi + 1;
                triangles[ti + 4] = triangles[ti + 1] = vi + meshWidth + 1;
                triangles[ti + 5] = vi + meshWidth + 2;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
    }

    void Update()
    {
        if (originalVertices == null) return;

        bool isAdvancing = beachWaves != null ? beachWaves.IsWaveAdvancing() : true;
        float waveStrength = beachWaves != null ? beachWaves.GetWaveStrength() : 1f;

        // Animate vertices
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 vertex = originalVertices[i];

            // Calculate distance from center for edge emphasis
            float distanceFromCenter = Vector2.Distance(new Vector2(vertex.x, vertex.z), Vector2.zero);
            float edgeMultiplier = Mathf.Lerp(0.3f, edgeWaveIntensity, distanceFromCenter / (meshSize * 0.5f));

            // Create wave pattern
            float wave1 = Mathf.Sin((vertex.x + Time.time * waveSpeed) * waveFrequency) * waveHeight;
            float wave2 = Mathf.Cos((vertex.z + Time.time * waveSpeed * 0.7f) * waveFrequency * 1.3f) * waveHeight * 0.5f;

            // Apply beach wave influence
            float beachWaveOffset = 0f;
            if (beachWaves != null)
            {
                beachWaveOffset = isAdvancing ? waveStrength * 0.2f : -waveStrength * 0.1f;
            }

            vertex.y = (wave1 + wave2 + beachWaveOffset) * edgeMultiplier;
            vertices[i] = vertex;
        }

        mesh.vertices = vertices;
        mesh.RecalculateNormals();
    }
}