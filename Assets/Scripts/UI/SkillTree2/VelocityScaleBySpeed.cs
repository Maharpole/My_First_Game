using UnityEngine;

public class VelocityScaleBySpeed : MonoBehaviour
{
    public Rigidbody rb; // optional if you use RB
    public float baseScale = 1f;
    public float scalePerSpeed = 0.02f; // how much size changes per unit speed
    public float minScale = 0.8f;
    public float maxScale = 1.3f;
    public bool enabledBySkill = false; // toggled on when node applies

    Vector3 _orig;
    Vector3 _lastPos;

    void Awake()
    {
        _orig = transform.localScale;
        if (rb == null) rb = GetComponent<Rigidbody>();
        _lastPos = transform.position;
    }

    void LateUpdate()
    {
        if (!enabledBySkill) return;
        float speed = 0f;
        if (rb != null) speed = rb.linearVelocity.magnitude;
        else // estimate from last frame movement
        {
            Vector3 pos = transform.position;
            speed = (pos - _lastPos).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
            _lastPos = pos;
        }
        float scaleMul = Mathf.Clamp(baseScale + speed * scalePerSpeed, minScale, maxScale);
        transform.localScale = _orig * scaleMul;
    }
}


