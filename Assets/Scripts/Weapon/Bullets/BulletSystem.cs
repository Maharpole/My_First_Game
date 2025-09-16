using UnityEngine;

public static class BulletSystem
{
    public static void FirePellet(Transform instigator, Transform muzzle, Vector3 dir, WeaponFireProfile weaponProfile, BulletProfile bullet)
    {
        dir = ApplyBloom(dir, bullet.bloomDegrees);

        if (bullet.mode == BulletProfile.Mode.Hitscan)
        {
            var origin = muzzle.position;
            float remaining = bullet.maxRange;
            int penetrations = Mathf.Max(0, bullet.penetrationCount);
            // Track enemies already hit by this pellet to avoid multi-hits through multiple colliders
            System.Collections.Generic.HashSet<int> hitEnemyIds = new System.Collections.Generic.HashSet<int>();
            // Guard: if starting inside a collider, perform a short back-cast then forward cast to get a valid hit
            float overlapRadius = bullet.castRadius > 0.0001f ? bullet.castRadius : 0.05f;
            var overlaps = Physics.OverlapSphere(origin, overlapRadius, bullet.hitMask, bullet.triggerInteraction);
            if (overlaps != null && overlaps.Length > 0)
            {
                for (int i = 0; i < overlaps.Length; i++)
                {
                    var col = overlaps[i]; if (col == null) continue;
                    if (instigator != null && col.transform.IsChildOf(instigator)) continue;
                    Vector3 backOrigin = origin - dir * (overlapRadius + 0.05f);
                    float dist = overlapRadius * 2f + 0.1f;
                    RaycastHit firstHit;
                    bool got = false;
                    if (bullet.castRadius > 0.0001f)
                    {
                        var hits2 = Physics.SphereCastAll(backOrigin, bullet.castRadius, dir, dist, bullet.hitMask, bullet.triggerInteraction);
                        if (hits2 != null && hits2.Length > 0)
                        {
                            System.Array.Sort(hits2, (a,b) => a.distance.CompareTo(b.distance));
                            for (int j = 0; j < hits2.Length; j++)
                            {
                                if (hits2[j].collider == null) continue;
                                if (instigator != null && hits2[j].collider.transform.IsChildOf(instigator)) continue;
                                firstHit = hits2[j]; got = true; goto PROCESS_INITIAL;
                            }
                        }
                    }
                    else
                    {
                        var hits2 = Physics.RaycastAll(backOrigin, dir, dist, bullet.hitMask, bullet.triggerInteraction);
                        if (hits2 != null && hits2.Length > 0)
                        {
                            System.Array.Sort(hits2, (a,b) => a.distance.CompareTo(b.distance));
                            for (int j = 0; j < hits2.Length; j++)
                            {
                                if (hits2[j].collider == null) continue;
                                if (instigator != null && hits2[j].collider.transform.IsChildOf(instigator)) continue;
                                firstHit = hits2[j]; got = true; goto PROCESS_INITIAL;
                            }
                        }
                    }
                    break;
                PROCESS_INITIAL:
                    if (got)
                    {
                        float traveled = Vector3.Distance(origin, firstHit.point);
                        SpawnTracer(bullet, origin, firstHit.point);
                        var ctx0 = new HitContext { instigator = instigator, weapon = muzzle, origin = origin, direction = dir, traveled = traveled, hit = firstHit, damage = bullet.baseDamage, profile = bullet, weaponProfile = weaponProfile };
                        ResolveHit(ref ctx0);
                        SpawnImpact(bullet, firstHit);
                        remaining -= traveled + 0.001f;
                        origin = firstHit.point + dir * 0.001f;
                        if (penetrations-- <= 0) return;
                    }
                }
            }

            while (remaining > 0f)
            {
                bool gotHit = false;
                RaycastHit chosen = default;
                if (bullet.castRadius > 0.0001f)
                {
                    var hits = Physics.SphereCastAll(origin, bullet.castRadius, dir, remaining, bullet.hitMask, bullet.triggerInteraction);
                    if (hits != null && hits.Length > 0)
                    {
                        System.Array.Sort(hits, (a,b) => a.distance.CompareTo(b.distance));
                        for (int i = 0; i < hits.Length; i++)
                        {
                            if (hits[i].collider == null) continue;
                            if (instigator != null && hits[i].collider.transform.IsChildOf(instigator)) continue;
                            chosen = hits[i]; gotHit = true; break;
                        }
                    }
                }
                else
                {
                    var hits = Physics.RaycastAll(origin, dir, remaining, bullet.hitMask, bullet.triggerInteraction);
                    if (hits != null && hits.Length > 0)
                    {
                        System.Array.Sort(hits, (a,b) => a.distance.CompareTo(b.distance));
                        for (int i = 0; i < hits.Length; i++)
                        {
                            if (hits[i].collider == null) continue;
                            if (instigator != null && hits[i].collider.transform.IsChildOf(instigator)) continue;
                            chosen = hits[i]; gotHit = true; break;
                        }
                    }
                }
                if (gotHit)
                {
                    float traveled = Vector3.Distance(origin, chosen.point);
                    // Draw tracer up to the impact so it lingers as bullet smoke
                    SpawnTracer(bullet, origin, chosen.point);
                    var ctx = new HitContext {
                        instigator = instigator, weapon = muzzle, origin = origin,
                        direction = dir, traveled = traveled, hit = chosen,
                        damage = bullet.baseDamage, profile = bullet, weaponProfile = weaponProfile
                    };
                    // Resolve the enemy root from the hit to prevent duplicate hits on the same target
                    EnemyHealth enemy = null;
                    if (chosen.collider != null)
                    {
                        enemy = chosen.collider.GetComponentInParent<EnemyHealth>();
                        if (enemy == null)
                        {
                            var proxy = chosen.collider.GetComponent<EnemyHitboxProxy>();
                            if (proxy != null) enemy = proxy.Resolve();
                        }
                        if (enemy == null && chosen.collider.CompareTag("Enemy"))
                        {
                            enemy = chosen.collider.GetComponent<EnemyHealth>() ?? chosen.collider.GetComponentInParent<EnemyHealth>();
                        }
                    }

                    bool firstTimeHitThisEnemy = true;
                    if (enemy != null)
                    {
                        int id = enemy.GetInstanceID();
                        if (hitEnemyIds.Contains(id)) firstTimeHitThisEnemy = false; else hitEnemyIds.Add(id);
                    }

                    if (firstTimeHitThisEnemy)
                    {
                        ResolveHit(ref ctx);
                        SpawnImpact(bullet, chosen);
                        // Only consume a penetration when we actually applied a unique enemy hit, or when it's world geometry
                        if (enemy != null)
                        {
                            if (penetrations > 0) penetrations -= 1;
                        }
                        else
                        {
                            // World/props stop unless penetration budget allows passing through
                            if (penetrations > 0) penetrations -= 1; else break;
                        }
                    }

                    remaining -= traveled + 0.001f;
                    origin = chosen.point + dir * 0.001f;

                    if (penetrations < 0) break;
                }
                else
                {
                    SpawnTracer(bullet, origin, origin + dir * remaining);
                    break;
                }
            }
        }
        else
        {
            if (bullet.projectilePrefab == null) return;
            var go = Object.Instantiate(bullet.projectilePrefab, muzzle.position, Quaternion.LookRotation(dir));
            var pc = go.GetComponent<ProjectileController>();
            if (pc == null) pc = go.AddComponent<ProjectileController>();
            pc.profile = bullet;
            pc.weaponProfile = weaponProfile;
            pc.instigator = instigator;
            pc.velocity = dir * bullet.speed;
        }
    }

    public static void ResolveHit(ref HitContext ctx)
    {
        var effects = ctx.weaponProfile != null ? ctx.weaponProfile.effects : null;
        if (effects != null)
        {
            for (int i = 0; i < effects.Count; i++)
                if (effects[i] != null) effects[i].Apply(ref ctx);
        }
    }

    public static void SpawnTracer(BulletProfile bullet, Vector3 start, Vector3 end)
    {
        if (bullet.tracerPrefab == null) return;
        var go = Object.Instantiate(bullet.tracerPrefab, start, Quaternion.identity);
        var lr = go.GetComponent<LineRenderer>();
        if (lr != null)
        {
            lr.positionCount = 2;
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);
        }
        Object.Destroy(go, Mathf.Max(0.01f, bullet.tracerLifetime));
    }

    public static void SpawnImpact(BulletProfile bullet, RaycastHit hit)
    {
        if (bullet.impactVFX != null && hit.collider != null)
        {
            var rot = Quaternion.LookRotation(hit.normal);
            var vfx = Object.Instantiate(bullet.impactVFX, hit.point, rot);
            var main = vfx.main; main.playOnAwake = false; main.simulationSpace = ParticleSystemSimulationSpace.World;
            vfx.Play(true);
            Object.Destroy(vfx.gameObject, vfx.main.duration + vfx.main.startLifetime.constantMax + 0.05f);
        }
    }

    static Vector3 ApplyBloom(Vector3 fwd, float degrees)
    {
        if (degrees <= 0.001f) return fwd;
        float rad = degrees * Mathf.Deg2Rad;
        Vector2 d = Random.insideUnitCircle * Mathf.Tan(rad);
        Vector3 up = Mathf.Abs(Vector3.Dot(fwd, Vector3.up)) > 0.99f ? Vector3.right : Vector3.up;
        Vector3 right = Vector3.Normalize(Vector3.Cross(up, fwd));
        up = Vector3.Normalize(Vector3.Cross(fwd, right));
        return Vector3.Normalize(fwd + right * d.x + up * d.y);
    }
}


