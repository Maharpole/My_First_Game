using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ClickShooter : MonoBehaviour
{
    [Header("References")] public Transform muzzle;
    public GameObject bulletPrefab;
    public LayerMask groundMask = ~0; // where the cursor ray can hit to aim

    [Header("Shooting")] public float bulletSpeed = 30f;
    public float fireRate = 6f; // shots per second
    [Tooltip("Rotate a target transform to face aim direction")] public bool faceAimDirection = true;
    [Tooltip("Transform that should rotate to face the aim. If null, will try Player.Instance, else this transform.")] public Transform faceTarget;
    [Tooltip("How fast to rotate toward aim direction (degrees/second). Set very high to snap.")] public float faceTurnSpeed = 360f;

    [Header("Indicator")] public Transform aimIndicator; // optional ring/arrow visual (use CrescentAimIndicator)
    public float indicatorRadius = 1.2f;

    float _nextFireTime;

    void Awake()
    {
        if (muzzle == null) muzzle = transform;
        if (faceTarget == null)
        {
            var p = Player.Instance ?? Object.FindFirstObjectByType<Player>();
            faceTarget = p != null ? p.transform : transform;
        }
    }

    void Update()
    {
        Vector3 aimPoint;
        if (TryGetMouseAimPoint(out aimPoint))
        {
            Vector3 dir = (aimPoint - muzzle.position);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
            {
                dir.Normalize();
                if (faceAimDirection && faceTarget != null)
                {
                    Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
                    if (faceTurnSpeed <= 0f) faceTarget.rotation = targetRot;
                    else faceTarget.rotation = Quaternion.RotateTowards(faceTarget.rotation, targetRot, faceTurnSpeed * Time.deltaTime);
                }
                UpdateIndicator(dir);
            }

            if (IsFirePressed() && Time.time >= _nextFireTime)
            {
                Fire(dir);
                _nextFireTime = Time.time + (fireRate > 0f ? 1f / fireRate : 0.2f);
            }
        }
    }

    void Fire(Vector3 dir)
    {
        if (bulletPrefab == null) return;
        Vector3 pos = muzzle != null ? muzzle.position : transform.position;
        var go = Instantiate(bulletPrefab, pos, Quaternion.LookRotation(dir));
        var rb = go.GetComponent<Rigidbody>();
        if (rb == null) rb = go.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.linearVelocity = dir * bulletSpeed;
    }

    void UpdateIndicator(Vector3 dir)
    {
        if (aimIndicator == null) return;
        var crescent = aimIndicator.GetComponent<CrescentAimIndicator>();
        if (crescent != null)
        {
            crescent.SetDirection(transform.position, dir, indicatorRadius);
        }
        else
        {
            aimIndicator.position = transform.position + new Vector3(dir.x, 0f, dir.z) * indicatorRadius;
            aimIndicator.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }
    }

    bool TryGetMouseAimPoint(out Vector3 point)
    {
        point = Vector3.zero;
        Camera cam = Camera.main;
        if (cam == null) return false;
#if ENABLE_INPUT_SYSTEM
        Vector2 mp = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
        Vector2 mp = Input.mousePosition;
#endif
        Ray ray = cam.ScreenPointToRay(mp);
        // Prefer a non-player ground hit: use RaycastAll to skip our own colliders and non-ground hits
        var hits = Physics.RaycastAll(ray, 500f, groundMask, QueryTriggerInteraction.Ignore);
        if (hits != null && hits.Length > 0)
        {
            System.Array.Sort(hits, (a,b) => a.distance.CompareTo(b.distance));
            for (int i = 0; i < hits.Length; i++)
            {
                var h = hits[i];
                if (h.collider == null) continue;
                // ignore anything on our hierarchy (weapon, player)
                if (h.collider.transform.IsChildOf(transform)) continue;
                point = h.point;
                return true;
            }
        }
        // Fallback: intersect with horizontal plane at player height to keep aim stable even if ray misses ground
        Plane plane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));
        if (plane.Raycast(ray, out float t))
        {
            point = ray.GetPoint(t);
            return true;
        }
        return false;
    }

    bool IsFirePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.isPressed;
#else
        return Input.GetMouseButton(0);
#endif
    }
}


