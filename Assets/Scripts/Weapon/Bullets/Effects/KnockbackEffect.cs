using UnityEngine;

[CreateAssetMenu(menuName="Combat/Effects/Knockback")]
public class KnockbackEffect : BulletEffect
{
    [System.Serializable]
    public class Params : EffectParams
    {
        public float force = 5f;
        public float upForce = 0f;
        public ForceMode mode = ForceMode.Impulse;
        [Tooltip("Multiply horizontal force by the hit context damage value.")] public bool scaleByDamage = false;
        [Tooltip("If true, use the surface normal instead of shot direction for push.")] public bool pushAlongSurfaceNormal = false;
    }

    public override EffectParams CreateDefaultParams() { return new Params(); }

    // Back-compat defaults (used if no params provided)
    public float force = 5f;
    public float upForce = 0f;
    public ForceMode mode = ForceMode.Impulse;
    public bool scaleByDamage = false;
    public bool pushAlongSurfaceNormal = false;

    public override void Apply(ref HitContext ctx)
    {
        if (ctx.hit.collider == null) return;
        var rb = ctx.hit.rigidbody;
        if (rb == null) rb = ctx.hit.collider.GetComponentInParent<Rigidbody>();
        var eh = ctx.hit.collider.GetComponentInParent<EnemyHealth>();
        float resist = (eh != null) ? eh.GetKnockbackMultiplier() : 1f;
        if (rb == null || rb.isKinematic)
        {
            // fallback to agent displacement if no dynamic RB
            var agent = ctx.hit.collider.GetComponentInParent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null && agent.enabled)
            {
                var p = _params as Params;
                bool useNormal = p != null ? p.pushAlongSurfaceNormal : pushAlongSurfaceNormal;
                Vector3 kdir = useNormal && ctx.hit.normal != Vector3.zero
                    ? ctx.hit.normal
                    : (ctx.direction.sqrMagnitude > 0.0001f ? ctx.direction : (ctx.hit.point - ctx.origin).normalized);
                float f = p != null ? p.force : force;
                ctx.weapon.GetComponent<MonoBehaviour>()?.StartCoroutine(KnockbackAgent(agent, kdir, f * resist, 0.12f));
            }
            return;
        }

        var pp = _params as Params;
        bool useNorm2 = pp != null ? pp.pushAlongSurfaceNormal : pushAlongSurfaceNormal;
        Vector3 dir = useNorm2 && ctx.hit.normal != Vector3.zero
            ? ctx.hit.normal
            : (ctx.direction.sqrMagnitude > 0.0001f ? ctx.direction : (ctx.hit.point - ctx.origin).normalized);
        float f2 = pp != null ? pp.force : force;
        float uf2 = pp != null ? pp.upForce : upForce;
        bool scale = pp != null ? pp.scaleByDamage : scaleByDamage;
        var modeToUse = pp != null ? pp.mode : mode;
        float magnitude = f2 * (scale ? Mathf.Max(1f, ctx.damage) : 1f) * resist;
        Vector3 impulse = dir * magnitude + Vector3.up * Mathf.Max(0f, uf2 * resist);
        rb.AddForce(impulse, modeToUse);
    }

    System.Collections.IEnumerator KnockbackAgent(UnityEngine.AI.NavMeshAgent agent, Vector3 dir, float distance, float time)
    {
        if (agent == null) yield break;
        bool wasEnabled = agent.enabled;
        if (wasEnabled)
        {
            agent.ResetPath();
            agent.isStopped = true;
            agent.updatePosition = false;
            agent.updateRotation = false;
            agent.enabled = false;
        }
        Transform target = agent.transform;
        Vector3 start = target.position;
        Vector3 end = start + new Vector3(dir.x, 0f, dir.z).normalized * Mathf.Max(0f, distance);
        float duration = Mathf.Max(0.01f, time);
        float t = 0f;
        while (t < duration && target != null)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / duration);
            target.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, a));
            yield return null;
        }
        if (target != null && wasEnabled)
        {
            agent.enabled = true;
            agent.isStopped = false;
            agent.updatePosition = true;
            agent.updateRotation = false;
        }
    }
}




