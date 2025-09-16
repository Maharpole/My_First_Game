using UnityEngine;

public class ProjectileController : MonoBehaviour
{
    public BulletProfile profile;
    public WeaponFireProfile weaponProfile;
    public Transform instigator;
    public Vector3 velocity;
    bool _hasImpacted;

    void Awake()
    {
        // Ensure projectile is free in world space and not driven by physics/legacy scripts
        if (transform.parent != null) transform.SetParent(null, true);
        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.detectCollisions = false; // collision is handled by raycasts below
        }
        var legacyBullet = GetComponent<Bullet>();
        if (legacyBullet != null) legacyBullet.enabled = false;
    }

    void Update()
    {
        if (profile == null) { gameObject.SetActive(false); return; }
        if (_hasImpacted) return;
        // Only apply gravity along world Y to avoid flipping forward direction when integrating
        if (profile.gravity > 0f)
        {
            velocity += Vector3.down * profile.gravity * Time.deltaTime;
        }
        var step = velocity * Time.deltaTime;
        Vector3 dir = velocity.normalized;

        // Guard: if starting or moving inside a collider, try to back-cast out to get a valid hit
        float overlapRadius = profile.castRadius > 0.0001f ? profile.castRadius : 0.02f;
        var overlaps = Physics.OverlapSphere(transform.position, overlapRadius, profile.hitMask, profile.triggerInteraction);
        if (overlaps != null && overlaps.Length > 0)
        {
            for (int i = 0; i < overlaps.Length; i++)
            {
                var col = overlaps[i];
                if (col == null) continue;
                if (instigator != null && col.transform.IsChildOf(instigator)) continue;
                Vector3 castDir = dir.sqrMagnitude > 0.0001f ? dir : transform.forward;
                Vector3 backOrigin = transform.position - castDir * Mathf.Max(0.05f, overlapRadius);
                float dist = Mathf.Max(0.1f, overlapRadius * 2f);
                bool got;
                RaycastHit outHit = default;
                if (profile.castRadius > 0.0001f)
                {
                    var hits2 = Physics.SphereCastAll(backOrigin, profile.castRadius, castDir, dist, profile.hitMask, profile.triggerInteraction);
                    got = false;
                    if (hits2 != null && hits2.Length > 0)
                    {
                        System.Array.Sort(hits2, (a,b) => a.distance.CompareTo(b.distance));
                        for (int j = 0; j < hits2.Length; j++)
                        {
                            if (hits2[j].collider == null) continue;
                            if (instigator != null && hits2[j].collider.transform.IsChildOf(instigator)) continue;
                            outHit = hits2[j]; got = true; break;
                        }
                    }
                }
                else
                {
                    var hits2 = Physics.RaycastAll(backOrigin, castDir, dist, profile.hitMask, profile.triggerInteraction);
                    got = false;
                    if (hits2 != null && hits2.Length > 0)
                    {
                        System.Array.Sort(hits2, (a,b) => a.distance.CompareTo(b.distance));
                        for (int j = 0; j < hits2.Length; j++)
                        {
                            if (hits2[j].collider == null) continue;
                            if (instigator != null && hits2[j].collider.transform.IsChildOf(instigator)) continue;
                            outHit = hits2[j]; got = true; break;
                        }
                    }
                }
                if (!got)
                {
                    // last resort: simple forward cast
                    Physics.Raycast(transform.position, castDir, out outHit, Mathf.Max(0.05f, overlapRadius * 2f), profile.hitMask, profile.triggerInteraction);
                }
                if (outHit.collider != null) { OnImpact(outHit); return; }
            }
        }

        bool gotHit = false;
        RaycastHit hit = default;
        // dir already computed above
        if (profile.castRadius > 0.0001f)
        {
            var hits = Physics.SphereCastAll(transform.position, profile.castRadius, dir, step.magnitude, profile.hitMask, profile.triggerInteraction);
            gotHit = false;
            if (hits != null && hits.Length > 0)
            {
                System.Array.Sort(hits, (a,b) => a.distance.CompareTo(b.distance));
                for (int i = 0; i < hits.Length; i++)
                {
                    if (hits[i].collider == null) continue;
                    if (instigator != null && hits[i].collider.transform.IsChildOf(instigator)) continue;
                    hit = hits[i]; gotHit = true; break;
                }
            }
        }
        else
        {
            var hits = Physics.RaycastAll(transform.position, dir, step.magnitude, profile.hitMask, profile.triggerInteraction);
            gotHit = false;
            if (hits != null && hits.Length > 0)
            {
                System.Array.Sort(hits, (a,b) => a.distance.CompareTo(b.distance));
                for (int i = 0; i < hits.Length; i++)
                {
                    if (hits[i].collider == null) continue;
                    if (instigator != null && hits[i].collider.transform.IsChildOf(instigator)) continue;
                    hit = hits[i]; gotHit = true; break;
                }
            }
        }
        if (gotHit) { OnImpact(hit); return; }

        // Advance strictly along current velocity; do not re-orient from parent/muzzle after spawn
        transform.position += step;
        if (dir.sqrMagnitude > 0.000001f)
        {
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }
    }

    void OnImpact(RaycastHit hit)
    {
        if (_hasImpacted) return;
        _hasImpacted = true;
        var ctx = new HitContext {
            instigator = instigator, weapon = transform, origin = transform.position,
            direction = velocity.sqrMagnitude > 0.0001f ? velocity.normalized : transform.forward,
            traveled = 0f, hit = hit,
            damage = profile.baseDamage, profile = profile, weaponProfile = weaponProfile
        };
        BulletSystem.ResolveHit(ref ctx);
        BulletSystem.SpawnImpact(profile, hit);
        DetachAndFadeTrails();
    }

    void DetachAndFadeTrails()
    {
        var trails = GetComponentsInChildren<TrailRenderer>(true);
        if (trails == null || trails.Length == 0) { Destroy(gameObject); enabled = false; return; }
        float maxTrail = 0f;
        bool hasRootTrail = false;
        for (int i = 0; i < trails.Length; i++)
        {
            var tr = trails[i]; if (tr == null) continue;
            maxTrail = Mathf.Max(maxTrail, tr.time);
            if (tr.transform == transform)
            {
                hasRootTrail = true;
            }
            else
            {
                tr.transform.SetParent(null, true);
                tr.autodestruct = true;
                tr.emitting = false;
                Object.Destroy(tr.gameObject, tr.time + 0.25f);
            }
        }
        if (hasRootTrail)
        {
            // Hide visuals and disable physics/scripts, let root trail fade then destroy self
            var meshRenderers = GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < meshRenderers.Length; i++) if (meshRenderers[i] != null) meshRenderers[i].enabled = false;
            var skinned = GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < skinned.Length; i++) if (skinned[i] != null) skinned[i].enabled = false;
            var sprite = GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < sprite.Length; i++) if (sprite[i] != null) sprite[i].enabled = false;
            var ps = GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < ps.Length; i++) if (ps[i] != null) ps[i].Stop(true, ParticleSystemStopBehavior.StopEmitting);
            var colliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++) if (colliders[i] != null) colliders[i].enabled = false;
            var rb = GetComponent<Rigidbody>(); if (rb != null) rb.isKinematic = true;
            // Stop emitting on root trails and ensure autodestruct
            for (int i = 0; i < trails.Length; i++)
            {
                var tr = trails[i]; if (tr == null) continue;
                tr.emitting = false; tr.autodestruct = true;
            }
            Destroy(gameObject, maxTrail + 0.25f);
            enabled = false;
        }
        else
        {
            Destroy(gameObject);
            enabled = false;
        }
    }
}


