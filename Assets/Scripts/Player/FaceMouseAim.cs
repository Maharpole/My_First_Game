using UnityEngine;

/// Rotates a target (default: this transform) to always face the mouse cursor,
/// projecting onto ground. Works regardless of NavMeshAgent/Rigidbody.
public class FaceMouseAim : MonoBehaviour
{
    [Header("Target & Speed")]
    public Transform target; // default to self
    [Tooltip("Degrees per second to turn toward the mouse")] public float faceTurnSpeed = 540f;

    [Header("Grounding & Masks")]
    [Tooltip("Layers considered ground for mouse projection")] public LayerMask groundMask = ~0;
    [Tooltip("Ignore these when raycasting (typically the player's own colliders)")] public LayerMask ignoreMask = 0;

    [Header("Options")]
    [Tooltip("Maintain upright rotation (Y-axis only)")] public bool yAxisOnly = true;
    [Tooltip("Do nothing if cursor projects too close to the target")] public float minAimDistance = 0.05f;

    Camera _cam;

    void Awake()
    {
        if (target == null) target = transform;
        _cam = Camera.main;
    }

    void LateUpdate()
    {
        if (_cam == null)
        {
            _cam = Camera.main;
            if (_cam == null) return;
        }

        if (!TryGetMouseGroundPoint(out var aimPoint)) return;

        Vector3 from = target.position;
        if (yAxisOnly)
        {
            aimPoint.y = from.y;
        }

        Vector3 toDir = aimPoint - from;
        if (yAxisOnly) toDir.y = 0f;
        float sqMag = toDir.sqrMagnitude;
        if (sqMag < minAimDistance * minAimDistance) return;

        Quaternion desired = Quaternion.LookRotation(toDir.normalized, Vector3.up);
        target.rotation = Quaternion.Slerp(target.rotation, desired, Mathf.Clamp01(faceTurnSpeed * Mathf.Deg2Rad * Time.deltaTime));
    }

    bool TryGetMouseGroundPoint(out Vector3 worldPoint)
    {
        worldPoint = Vector3.zero;
        Ray ray = _cam.ScreenPointToRay(Input.mousePosition);
        var hits = Physics.RaycastAll(ray, 1000f, groundMask, QueryTriggerInteraction.Ignore);
        if (hits != null && hits.Length > 0)
        {
            float bestDist = float.MaxValue;
            bool found = false;
            for (int i = 0; i < hits.Length; i++)
            {
                var h = hits[i];
                // Skip if hit object is on ignoreMask
                if (((1 << h.collider.gameObject.layer) & ignoreMask) != 0) continue;
                // Skip self/children
                if (target != null && h.collider.transform.IsChildOf(target)) continue;
                if (h.distance < bestDist)
                {
                    bestDist = h.distance;
                    worldPoint = h.point;
                    found = true;
                }
            }
            if (found) return true;
        }

        // Fallback: intersect ray with plane at target height
        Plane plane = new Plane(Vector3.up, new Vector3(0f, target.position.y, 0f));
        if (plane.Raycast(ray, out float enter))
        {
            worldPoint = ray.origin + ray.direction * enter;
            return true;
        }

        return false;
    }
}


