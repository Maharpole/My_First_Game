using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Minimal, scene-only controller: keeps this object on a circle around the player,
// pointing toward the mouse. Attach to your existing world-space crescent UI object.
public class SimpleAimIndicator : MonoBehaviour
{
    [Header("Target")] public Transform player; // if null, will try Player.Instance
    [Header("Circle")] public float radius = 1.2f; public float heightOffset = 0.05f;
    [Header("Orientation")] public Vector3 rotationOffsetEuler = new Vector3(90f, 0f, 0f); // tilt flat on ground by default
    [Header("Size")] public float scale = 1f; // overall scale of the indicator visual
    [Header("Raycast")] public LayerMask groundMask = ~0; // ground layers only
    [Header("Stability")] public float slerpSpeed = 20f; public bool useStablePlaneAim = true;

    void Awake()
    {
        if (player == null)
        {
            var p = Player.Instance ?? Object.FindFirstObjectByType<Player>();
            player = p != null ? p.transform : null;
        }
        ApplyScale();
    }

    void LateUpdate()
    {
        if (player == null) return;

        Vector3 aimPoint;
        if (!TryGetMousePoint(player.position.y, out aimPoint)) return;

        Vector3 dir = aimPoint - player.position; dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        dir.Normalize();
        if (slerpSpeed > 0f)
        {
            Vector3 flatForward = transform.forward; flatForward.y = 0f;
            if (flatForward.sqrMagnitude > 0.0001f)
            {
                flatForward.Normalize();
                dir = Vector3.Slerp(flatForward, dir, 1f - Mathf.Exp(-slerpSpeed * Time.unscaledDeltaTime));
            }
        }

        transform.position = player.position + dir * radius + Vector3.up * heightOffset;
        transform.rotation = Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(rotationOffsetEuler);
    }

    void OnValidate()
    {
        ApplyScale();
    }

    void ApplyScale()
    {
        float s = Mathf.Max(0.001f, scale);
        var rt = GetComponent<RectTransform>();
        if (rt != null) rt.localScale = Vector3.one * s;
        else transform.localScale = Vector3.one * s;
    }

    bool TryGetMousePoint(float planeY, out Vector3 point)
    {
        point = Vector3.zero; var cam = Camera.main; if (cam == null) return false;
#if ENABLE_INPUT_SYSTEM
        Vector2 mp = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
        Vector2 mp = Input.mousePosition;
#endif
        Ray ray = cam.ScreenPointToRay(mp);
        if (!useStablePlaneAim)
        {
            var hits = Physics.RaycastAll(ray, 1000f, groundMask, QueryTriggerInteraction.Ignore);
            if (hits != null && hits.Length > 0)
            {
                System.Array.Sort(hits, (a,b) => a.distance.CompareTo(b.distance));
                for (int i = 0; i < hits.Length; i++)
                {
                    var h = hits[i]; if (h.collider == null) continue;
                    if (player != null && h.collider.transform.IsChildOf(player)) continue;
                    point = h.point; return true;
                }
            }
        }
        // Stable: plane at player height
        {
            Plane plane = new Plane(Vector3.up, new Vector3(0f, planeY, 0f));
            if (plane.Raycast(ray, out float t)) { point = ray.GetPoint(t); return true; }
        }
        return false;
    }
}


