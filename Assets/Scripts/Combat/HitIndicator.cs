using UnityEngine;

/// Simple pooled ground indicator using a quad + SDF shader.
public class HitIndicator : MonoBehaviour
{
    public MeshRenderer meshRenderer;
    public float radius = 2f;
    public float angleDeg = 90f;
    public float rotationDeg = 0f;
    public float progress = 0f; // 0..1 inner fill
    public Color baseColor = new Color(1f,0f,0f,0.25f);
    public Color innerColor = new Color(1f,0.3f,0.3f,0.6f);

    MaterialPropertyBlock _mpb;

    void Awake()
    {
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
        if (_mpb == null) _mpb = new MaterialPropertyBlock();
        // Ensure quad is flat on ground; authored with rotation handled by owner
        transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
        Apply();
    }

    public void Configure(float worldRadius, float angle, float rotation, float startProgress, Color baseCol, Color innerCol)
    {
        radius = Mathf.Max(0.01f, worldRadius);
        angleDeg = angle;
        rotationDeg = rotation;
        progress = Mathf.Clamp01(startProgress);
        baseColor = baseCol;
        innerColor = innerCol;
        transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
        Apply();
    }

    public void SetProgress(float p)
    {
        progress = Mathf.Clamp01(p);
        Apply();
    }

    void Apply()
    {
        if (meshRenderer == null) return;
        _mpb.SetFloat("_AngleDeg", angleDeg);
        _mpb.SetFloat("_RotationDeg", rotationDeg);
        _mpb.SetFloat("_Progress", progress);
        _mpb.SetColor("_BaseColor", baseColor);
        _mpb.SetColor("_InnerColor", innerColor);
        meshRenderer.SetPropertyBlock(_mpb);
    }
}


