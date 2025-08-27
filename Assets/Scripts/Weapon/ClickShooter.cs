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

    [Header("VFX")] public ParticleSystem muzzleFlash;
    [Tooltip("If no ParticleSystem is assigned, search under the muzzle for a child named this")] public string muzzleFlashChildName = "MuzzleFlash";
    public bool autoFindMuzzleFlash = true;

    [Header("Audio")] public AudioSource audioSource;
    [Tooltip("Clips randomly chosen when firing")] public AudioClip[] fireClips;
    [Range(0f,1f)] public float fireVolume = 1f;
    [Tooltip("Random pitch range for variation (x=min, y=max)")] public Vector2 firePitchRange = new Vector2(1f, 1f);

    float _nextFireTime;

    void Awake()
    {
        if (muzzle == null) muzzle = transform;
        if (faceTarget == null)
        {
            var p = Player.Instance ?? Object.FindFirstObjectByType<Player>();
            faceTarget = p != null ? p.transform : transform;
        }
        if (muzzleFlash == null && autoFindMuzzleFlash)
        {
            if (muzzle != null)
            {
                // Try exact name match first
                var t = muzzle.Find(muzzleFlashChildName);
                if (t != null) muzzleFlash = t.GetComponent<ParticleSystem>();
                // Fallback: any ParticleSystem under muzzle
                if (muzzleFlash == null) muzzleFlash = muzzle.GetComponentInChildren<ParticleSystem>(true);
            }
        }
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
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
            }

            if (!UIInputBlocker.IsPointerBlocking && IsFirePressed() && Time.time >= _nextFireTime)
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

        TriggerMuzzleFlash(dir);
        PlayFireSound();
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

    void TriggerMuzzleFlash(Vector3 dir)
    {
        if (muzzleFlash == null) return;
        Transform t = muzzleFlash.transform;
        if (muzzle != null)
        {
            t.position = muzzle.position;
            t.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }
        var main = muzzleFlash.main;
        main.playOnAwake = false;
        muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        muzzleFlash.Play(true);
    }

    void PlayFireSound()
    {
        if (audioSource == null) return;
        if (fireClips == null || fireClips.Length == 0) return;
        var clip = fireClips[Random.Range(0, fireClips.Length)];
        if (clip == null) return;
        float minP = Mathf.Min(firePitchRange.x, firePitchRange.y);
        float maxP = Mathf.Max(firePitchRange.x, firePitchRange.y);
        audioSource.pitch = Random.Range(minP, maxP);
        audioSource.PlayOneShot(clip, Mathf.Clamp01(fireVolume));
    }
}


