using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class ArenaGenerator : MonoBehaviour
{
    [Header("Arena Boyutları")]
    [Range(2f, 20f)]
    public float radius = 6f;

    [Range(0.2f, 5f)]
    public float depth = 1.5f;

    [Range(0.1f, 3f)]
    public float wallHeight = 1f;

    [Range(12, 128)]
    public int segments = 48;

    [Range(4, 64)]
    public int rings = 16;

    [Header("Materyal")]
    public Material arenaMaterial;

    private Mesh generatedMesh;
    private bool hasFixedVisuals = false;

    private void Awake()
    {
        GenerateArena();
    }

    private void Start()
    {
        GenerateArena();
    }

    private void OnEnable()
    {
        GenerateArena();
    }

    private void OnValidate()
    {
        GenerateArena();
    }

    private void Update()
    {
        if (!hasFixedVisuals && Application.isPlaying)
        {
            // Arkaplan rengini koyu yap (kamuflajı önler)
            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.1f, 0.15f, 0.2f);
            }

            // Eğer segments Inspector'dan 12 yapılmışsa düzelt
            if (segments < 48)
            {
                segments = 64;
                GenerateArena();
            }

            // Renk/tint zorlaması kaldırıldı — orijinal / URP materyal kalsın
            hasFixedVisuals = true;
        }
    }

    [ContextMenu("Generate Arena")]
    public void GenerateArena()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        MeshCollider meshCollider = GetComponent<MeshCollider>();
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();

        if (meshFilter == null || meshCollider == null || meshRenderer == null) return;

        if (generatedMesh == null)
        {
            generatedMesh = new Mesh();
            generatedMesh.name = "Beyblade_Arena_Mesh";
        }
        else
        {
            generatedMesh.Clear();
        }

        int totalRings = rings + 1;
        int vertCount = 1 + totalRings * segments;
        Vector3[] vertices = new Vector3[vertCount];
        Vector2[] uvs = new Vector2[vertCount];

        vertices[0] = Vector3.zero;
        uvs[0] = new Vector2(0.5f, 0.5f);

        int vIdx = 1;
        for (int r = 1; r <= rings; r++)
        {
            float rFrac = (float)r / rings;
            float curRadius = rFrac * radius;
            float curHeight = depth * (rFrac * rFrac);

            for (int s = 0; s < segments; s++)
            {
                float angle = (float)s / segments * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * curRadius;
                float z = Mathf.Sin(angle) * curRadius;

                vertices[vIdx] = new Vector3(x, curHeight, z);
                uvs[vIdx] = new Vector2(0.5f + (x / (radius * 2f)), 0.5f + (z / (radius * 2f)));
                vIdx++;
            }
        }

        for (int s = 0; s < segments; s++)
        {
            float angle = (float)s / segments * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;

            vertices[vIdx] = new Vector3(x, depth + wallHeight, z);
            uvs[vIdx] = new Vector2(0.5f + (x / (radius * 2f)), 0.5f + (z / (radius * 2f)));
            vIdx++;
        }

        // TERS YÜZEY DÜZELTMESİ (Işık ve Gölgeleri Bozan Hatayı Çözer)
        // Yüzeylerin yukarı (kameraya) bakması için doğru üçgen çizimi
        int triCount = (segments * 3) + ((rings - 1) * segments * 6) + (segments * 6);
        int[] triangles = new int[triCount]; 
        int tIdx = 0;

        // Merkez Fan
        for (int s = 0; s < segments; s++)
        {
            int current = 1 + s;
            int next = 1 + ((s + 1) % segments);
            
            // YUKARI BAKAN ÇİZİM (0, current, next)
            triangles[tIdx++] = 0;
            triangles[tIdx++] = current;
            triangles[tIdx++] = next;
        }

        // Halkalar
        for (int r = 0; r < rings - 1; r++)
        {
            int innerStart = 1 + r * segments;
            int outerStart = 1 + (r + 1) * segments;

            for (int s = 0; s < segments; s++)
            {
                int nextS = (s + 1) % segments;
                int in1 = innerStart + s;
                int in2 = innerStart + nextS;
                int out1 = outerStart + s;
                int out2 = outerStart + nextS;

                // YUKARI BAKAN DÖRTGEN
                triangles[tIdx++] = in1;
                triangles[tIdx++] = in2;
                triangles[tIdx++] = out2;

                triangles[tIdx++] = in1;
                triangles[tIdx++] = out2;
                triangles[tIdx++] = out1;
            }
        }

        // Dış Duvar (Rim)
        int rimInnerStart = 1 + (rings - 1) * segments;
        int rimOuterStart = 1 + rings * segments;

        for (int s = 0; s < segments; s++)
        {
            int nextS = (s + 1) % segments;
            int in1 = rimInnerStart + s;
            int in2 = rimInnerStart + nextS;
            int out1 = rimOuterStart + s;
            int out2 = rimOuterStart + nextS;

            // YUKARI BAKAN DÖRTGEN
            triangles[tIdx++] = in1;
            triangles[tIdx++] = in2;
            triangles[tIdx++] = out2;

            triangles[tIdx++] = in1;
            triangles[tIdx++] = out2;
            triangles[tIdx++] = out1;
        }

        generatedMesh.vertices = vertices;
        generatedMesh.uv = uvs;
        generatedMesh.triangles = triangles;
        generatedMesh.RecalculateNormals(); // <--- İŞTE BÜTÜN SORUN BUYDU!
        generatedMesh.RecalculateBounds();

        meshFilter.sharedMesh = generatedMesh;

        // FİZİK DÜZELTMESİ
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = generatedMesh;

        if (arenaMaterial != null)
        {
            meshRenderer.sharedMaterial = arenaMaterial;
        }
        else
        {
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpShader == null) urpShader = Shader.Find("Standard");

            if (urpShader != null)
            {
                Material defaultMat = new Material(urpShader);
                defaultMat.SetColor("_BaseColor", new Color(0.8f, 0.1f, 0.15f));
                defaultMat.SetColor("_Color", new Color(0.8f, 0.1f, 0.15f));
                meshRenderer.sharedMaterial = defaultMat;
            }
        }
    }
}
