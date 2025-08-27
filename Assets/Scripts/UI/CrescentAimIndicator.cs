using UnityEngine;

// Draws a ring-sector (crescent-like) arrow that points with its opening forward.
// Built as a simple mesh in XZ plane; rotate/position from an external controller.
public class CrescentAimIndicator : MonoBehaviour
{
    [Header("Shape")] public float outerRadius = 1.2f;
    public float thickness = 0.35f; // outerRadius - innerRadius
    [Range(10f, 340f)] public float arcDegrees = 110f;
    [Range(4, 128)] public int segments = 32;
    public float heightOffset = 0.05f;

    [Header("Appearance")] public Color color = new Color(1f, 1f, 1f, 0.85f);
    public Material material; // optional; if null, a default unlit material will be created

    MeshFilter _mf; MeshRenderer _mr; Mesh _mesh;

    void Awake()
    {
        _mf = GetComponent<MeshFilter>();
        if (_mf == null) _mf = gameObject.AddComponent<MeshFilter>();
        _mr = GetComponent<MeshRenderer>();
        if (_mr == null) _mr = gameObject.AddComponent<MeshRenderer>();
        if (material == null)
        {
            var mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = color;
            material = mat;
        }
        _mr.sharedMaterial = material;
        RebuildMesh();
    }

    void OnValidate()
    {
        if (outerRadius <= 0.01f) outerRadius = 0.01f;
        if (thickness <= 0.01f) thickness = 0.01f;
        if (segments < 4) segments = 4;
        if (Application.isPlaying && _mf != null) RebuildMesh();
    }

    public void RebuildMesh()
    {
        if (_mesh == null)
        {
            _mesh = new Mesh();
            _mesh.name = "CrescentIndicatorMesh";
        }
        float innerR = Mathf.Max(0.001f, outerRadius - thickness);
        int vCount = (segments + 1) * 2;
        Vector3[] verts = new Vector3[vCount];
        Color[] colors = new Color[vCount];
        int[] tris = new int[segments * 6];

        float halfArc = Mathf.Deg2Rad * (arcDegrees * 0.5f);
        int vi = 0; int ti = 0;
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments; // 0..1
            float ang = Mathf.Lerp(-halfArc, halfArc, t);
            float c = Mathf.Cos(ang);
            float s = Mathf.Sin(ang);
            // In XZ plane: forward = +Z
            verts[vi] = new Vector3(innerR * s, heightOffset, innerR * c);
            colors[vi] = color; vi++;
            verts[vi] = new Vector3(outerRadius * s, heightOffset, outerRadius * c);
            colors[vi] = color; vi++;

            if (i < segments)
            {
                int baseIndex = i * 2;
                // Quad: (in_i, out_i, in_i+1) and (out_i, out_i+1, in_i+1)
                tris[ti++] = baseIndex;
                tris[ti++] = baseIndex + 1;
                tris[ti++] = baseIndex + 2;
                tris[ti++] = baseIndex + 1;
                tris[ti++] = baseIndex + 3;
                tris[ti++] = baseIndex + 2;
            }
        }

        _mesh.Clear();
        _mesh.vertices = verts;
        _mesh.colors = colors;
        _mesh.triangles = tris;
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();
        _mf.sharedMesh = _mesh;
    }

    // Update indicator placement from player position and aim direction.
    public void SetDirection(Vector3 playerPos, Vector3 dirNormalized, float orbitRadius)
    {
        outerRadius = orbitRadius; // keep mesh scale in sync if desired
        transform.position = playerPos + dirNormalized * orbitRadius + new Vector3(0f, heightOffset, 0f);
        transform.rotation = Quaternion.LookRotation(dirNormalized, Vector3.up);
    }
}


