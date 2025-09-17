using UnityEngine;
using UnityEngine.AI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ClickShooter : MonoBehaviour
{
    [Header("References")] public Transform muzzle;
    public GameObject bulletPrefab;
    public LayerMask groundMask = ~0; // where the cursor ray can hit to aim
    [Header("State")] public bool isArmed = false;
    [Header("Profiles")] public WeaponFireProfile profile;

    public enum FireMode { Projectile, Hitscan }

    [Header("Shooting")] public FireMode fireMode = FireMode.Projectile;
    public float bulletSpeed = 30f;
    public float fireRate = 6f; // shots per second
    [Tooltip("Rotate a target transform to face aim direction")] public bool faceAimDirection = false;
    [Tooltip("Transform that should rotate to face the aim. If null, will try Player.Instance, else this transform.")] public Transform faceTarget;
    [Tooltip("How fast to rotate toward aim direction (degrees/second). Set very high to snap.")] public float faceTurnSpeed = 360f;
    [Tooltip("Yaw offset in degrees applied when rotating the faceTarget toward aim (useful if model forward isn't +Z)")] public float aimYawOffsetDegrees = 0f;

    [Header("Aim")] public AimProvider aim; public bool useAimProvider = true;
    [Tooltip("Flatten aim to ground plane (top-down)")] public bool flattenAimToGroundPlane = true;

    [Header("Hitscan")] public int hitscanDamage = 10;
    public float hitscanMaxRange = 100f;
    [Tooltip("Cone half-angle in degrees for random bloom around the aim direction")] public float hitscanBloomDegrees = 0f;
    public LayerMask hitMask = ~0;
    [Tooltip("If true, ray can pass through multiple targets up to Max Penetrations")] public bool hitscanPenetrate = false;
    [Min(0)] public int hitscanMaxPenetrations = 0;
    [Tooltip("Impulse applied to rigidbodies on hit")] public float hitscanKnockback = 0f;
    public float hitscanKnockbackUp = 0f;

    [Header("Hitscan VFX")] public GameObject tracerPrefab;
    [Tooltip("If no prefab, no tracer will be shown. Prefab can contain a LineRenderer or Trail.")]
    public float tracerLifetime = 0.06f;
    [Tooltip("Impact effect spawned at hit point (optional)")] public ParticleSystem impactVFX;

    [Header("VFX")] public ParticleSystem muzzleFlash;
    [Tooltip("If no ParticleSystem is assigned, search under the muzzle for a child named this")] public string muzzleFlashChildName = "MuzzleFlash";
    public bool autoFindMuzzleFlash = true;

    [Header("Audio")] public AudioSource audioSource;
    [Tooltip("Clips randomly chosen when firing")] public AudioClip[] fireClips;
    [Range(0f,1f)] public float fireVolume = 1f;
    [Tooltip("Random pitch range for variation (x=min, y=max)")] public Vector2 firePitchRange = new Vector2(1f, 1f);

    [Header("Debug Gizmos")] public bool drawGizmos = true;
    [Tooltip("Draw even when not selected")] public bool gizmosAlways = false;
    [Tooltip("Length of the muzzle forward ray")] public float gizmoRayLength = 1.5f;
    public Color gizmoMuzzleColor = Color.cyan;
    public Color gizmoAimColor = new Color(1f, 0.3f, 0.2f, 1f);
    public Color gizmoFaceColor = Color.yellow;

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

    void OnDrawGizmos()
    {
        if (!drawGizmos || !gizmosAlways) return;
        DrawDebugGizmos();
    }

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        DrawDebugGizmos();
    }

    void Update()
    {
        if (!isArmed) return;
        Vector3 aimPoint;
        bool hasAim = false;
        if (useAimProvider && (aim != null || TryFindAimProvider(out aim)))
        {
            aimPoint = aim.AimPoint;
            hasAim = true;
        }
        else
        {
            hasAim = TryGetMouseAimPoint(out aimPoint);
        }
        if (hasAim)
        {
            Vector3 origin = (muzzle != null ? muzzle.position : transform.position);
            Vector3 dir = (aimPoint - origin);
            if (flattenAimToGroundPlane) dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
            {
                dir.Normalize();
                if (faceAimDirection && faceTarget != null && faceTarget != transform)
                {
                    Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
                    if (Mathf.Abs(aimYawOffsetDegrees) > 0.001f)
                    {
                        targetRot = Quaternion.AngleAxis(aimYawOffsetDegrees, Vector3.up) * targetRot;
                    }
                    if (faceTurnSpeed <= 0f) faceTarget.rotation = targetRot;
                    else faceTarget.rotation = Quaternion.RotateTowards(faceTarget.rotation, targetRot, faceTurnSpeed * Time.deltaTime);
                }
            }

            if (isArmed && muzzle != null && !UIInputBlocker.IsPointerBlocking && IsFirePressed() && Time.time >= _nextFireTime)
            {
                // New unified path using profiles
                if (profile != null && profile.bullet != null)
                {
                    Vector3 norm = dir.normalized;
                    int pellets = Mathf.Max(1, profile.pellets);
                    for (int i = 0; i < pellets; i++)
                    {
                        Vector3 shot = ApplySpread(norm, profile.extraPelletSpread);
                        BulletSystem.FirePellet(transform.root, muzzle, shot, profile, profile.bullet);
                    }
                    TriggerMuzzleFlash(norm);
                    PlayFireSound();
                    float rate = profile.fireRate > 0f ? profile.fireRate : fireRate;
                    _nextFireTime = Time.time + (rate > 0f ? 1f / rate : 0.2f);
                }
                else
                {
                    // Fallback to legacy modes for older weapons without profiles
                    if (fireMode == FireMode.Projectile) FireProjectile(dir);
                    else FireHitscan(dir);
                    _nextFireTime = Time.time + (fireRate > 0f ? 1f / fireRate : 0.2f);
                }
            }
        }
    }
    bool TryFindAimProvider(out AimProvider provider)
    {
        provider = aim;
        if (provider != null) return true;
        provider = GetComponentInParent<AimProvider>();
        if (provider != null) { aim = provider; return true; }
        return false;
    }

    void FireProjectile(Vector3 dir)
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

    void FireHitscan(Vector3 dir)
    {
        // Apply bloom by rotating within a cone around dir
        Vector3 shootDir = ApplyBloom(dir.normalized, hitscanBloomDegrees);
        Vector3 origin = muzzle != null ? muzzle.position : transform.position;

        Vector3 endPoint = origin + shootDir * Mathf.Max(0.01f, hitscanMaxRange);
        if (!hitscanPenetrate)
        {
            if (Physics.Raycast(origin, shootDir, out var hit, Mathf.Max(0.01f, hitscanMaxRange), hitMask, QueryTriggerInteraction.Ignore))
            {
                ApplyHitEffects(hit, shootDir);
                endPoint = hit.point;
            }
        }
        else
        {
            var hits = Physics.RaycastAll(origin, shootDir, Mathf.Max(0.01f, hitscanMaxRange), hitMask, QueryTriggerInteraction.Ignore);
            if (hits != null && hits.Length > 0)
            {
                System.Array.Sort(hits, (a,b) => a.distance.CompareTo(b.distance));
                int remaining = hitscanMaxPenetrations <= 0 ? hits.Length : Mathf.Min(hits.Length, hitscanMaxPenetrations + 1);
                for (int i = 0; i < remaining; i++)
                {
                    ApplyHitEffects(hits[i], shootDir);
                }
                endPoint = hits[Mathf.Clamp(remaining - 1, 0, hits.Length - 1)].point;
            }
        }

        SpawnTracer(origin, endPoint);
        TriggerMuzzleFlash(shootDir);
        PlayFireSound();
    }

    void DrawDebugGizmos()
    {
        Transform mz = muzzle != null ? muzzle : transform;
        Gizmos.color = gizmoMuzzleColor;
        Gizmos.DrawRay(mz.position, mz.forward * Mathf.Max(0.1f, gizmoRayLength));

        Vector3 aimPoint;
        if (TryGetMouseAimPoint(out aimPoint))
        {
            Gizmos.color = gizmoAimColor;
            Gizmos.DrawSphere(aimPoint, 0.05f);
            Gizmos.DrawLine(mz.position, aimPoint);
        }

        if (faceTarget != null)
        {
            Gizmos.color = gizmoFaceColor;
            Gizmos.DrawRay(faceTarget.position, faceTarget.forward * 0.8f);
        }
    }

    Vector3 ApplyBloom(Vector3 forward, float degrees)
    {
        if (degrees <= 0.001f) return forward;
        // Sample a random rotation around an axis perpendicular to forward
        // Use a random point on a unit disk and map to a small-angle rotation
        float rad = degrees * Mathf.Deg2Rad;
        // Cosine-weighted small angle sampling: approximate by uniform disk scaled by rad
        Vector2 d = Random.insideUnitCircle * Mathf.Tan(rad);
        // Build an orthonormal basis (right, up) around forward
        Vector3 up = Vector3.up;
        if (Mathf.Abs(Vector3.Dot(forward, up)) > 0.99f) up = Vector3.right;
        Vector3 right = Vector3.Normalize(Vector3.Cross(up, forward));
        up = Vector3.Normalize(Vector3.Cross(forward, right));
        Vector3 deviated = Vector3.Normalize(forward + right * d.x + up * d.y);
        return deviated;
    }

    Vector3 ApplySpread(Vector3 forward, float degrees)
    {
        if (degrees <= 0.001f) return forward;
        float rad = degrees * Mathf.Deg2Rad;
        Vector2 d = Random.insideUnitCircle * Mathf.Tan(rad);
        Vector3 up = Vector3.up;
        if (Mathf.Abs(Vector3.Dot(forward, up)) > 0.99f) up = Vector3.right;
        Vector3 right = Vector3.Normalize(Vector3.Cross(up, forward));
        up = Vector3.Normalize(Vector3.Cross(forward, right));
        return Vector3.Normalize(forward + right * d.x + up * d.y);
    }

    void ApplyHitEffects(RaycastHit hit, Vector3 shootDir)
    {
        if (hit.collider == null) return;
        // Find EnemyHealth via collider, proxy, or parent
        EnemyHealth enemyHealth = null;
        if (hit.collider.CompareTag("Enemy"))
        {
            enemyHealth = hit.collider.GetComponent<EnemyHealth>();
            if (enemyHealth == null) enemyHealth = hit.collider.GetComponentInParent<EnemyHealth>();
        }
        else
        {
            var proxy = hit.collider.GetComponent<EnemyHitboxProxy>();
            if (proxy != null) enemyHealth = proxy.Resolve();
            if (enemyHealth == null) enemyHealth = hit.collider.GetComponentInParent<EnemyHealth>();
        }

        if (enemyHealth != null)
        {
            int dmg = Mathf.Max(1, hitscanDamage);
            // MMFeedbacks: spawn floating text at hit point on channel 0
            var channel = new MoreMountains.Feedbacks.MMChannelData(MoreMountains.Feedbacks.MMChannelModes.Int, 0, null);
            MoreMountains.Feedbacks.MMFloatingTextSpawnEvent.Trigger(channel, hit.point, dmg.ToString(), Vector3.up, 1f);
            enemyHealth.TakeDamage(dmg);
        }

        // Knockback
        if (hitscanKnockback > 0f)
        {
            // Use planar direction for stability
            Vector3 planar = new Vector3(shootDir.x, 0f, shootDir.z).normalized;
            var rb = hit.rigidbody ?? hit.collider.GetComponentInParent<Rigidbody>();
            var agent = hit.collider.GetComponentInParent<NavMeshAgent>();
            // enemy-specific resistance scaling
            var eh = hit.collider.GetComponentInParent<EnemyHealth>();
            float resist = eh != null ? eh.GetKnockbackMultiplier() : 1f;
            if (rb != null && !rb.isKinematic)
            {
                // VelocityChange ensures mass-independent, consistent knockback
                if (planar.sqrMagnitude > 0.0001f) rb.AddForce(planar * (hitscanKnockback * resist), ForceMode.VelocityChange);
                if (hitscanKnockbackUp > 0f) rb.AddForce(Vector3.up * (hitscanKnockbackUp * resist), ForceMode.VelocityChange);
            }
            else if (agent != null && agent.enabled)
            {
                // Apply agent knockback as a short displacement
                StartCoroutine(KnockbackAgent(agent, planar, hitscanKnockback * resist, 0.12f));
            }
        }

        // Impact VFX
        if (impactVFX != null)
        {
            var vfx = Instantiate(impactVFX, hit.point, Quaternion.LookRotation(hit.normal));
            var main = vfx.main; main.playOnAwake = false; main.simulationSpace = ParticleSystemSimulationSpace.World;
            vfx.Play(true);
            Destroy(vfx.gameObject, vfx.main.duration + vfx.main.startLifetime.constantMax + 0.05f);
        }
    }

    System.Collections.IEnumerator KnockbackAgent(NavMeshAgent agent, Vector3 dir, float distance, float time)
    {
        if (agent == null) yield break;
        Transform target = agent.transform;
        bool wasEnabled = agent.enabled;
        if (wasEnabled)
        {
            agent.ResetPath();
            agent.isStopped = true;
            agent.updatePosition = false;
            agent.updateRotation = false;
            agent.enabled = false;
        }
        Vector3 start = target.position;
        Vector3 end = start + (dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.forward) * Mathf.Max(0f, distance);
        float duration = Mathf.Max(0.01f, time);
        float t = 0f;
        while (t < duration && target != null)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / duration);
            target.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, a));
            yield return null;
        }
        if (target != null)
        {
            if (wasEnabled)
            {
                agent.enabled = true;
                agent.isStopped = false;
                agent.updatePosition = true;
                agent.updateRotation = false;
            }
        }
    }

    void SpawnTracer(Vector3 start, Vector3 end)
    {
        if (tracerPrefab == null) return;
        var go = Instantiate(tracerPrefab, start, Quaternion.identity);
        // If prefab uses LineRenderer, set positions
        var lr = go.GetComponent<LineRenderer>();
        if (lr != null)
        {
            lr.positionCount = 2;
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);
        }
        // If it uses a TrailRenderer, position and orient forward toward end
        var tr = go.GetComponent<TrailRenderer>();
        if (tr != null)
        {
            go.transform.position = start;
            go.transform.rotation = Quaternion.LookRotation((end - start).normalized, Vector3.up);
        }
        Destroy(go, Mathf.Max(0.01f, tracerLifetime));
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


