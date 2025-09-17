using UnityEngine;
using MoreMountains.Feedbacks;

[CreateAssetMenu(menuName="Combat/Effects/Damage")]
public class DamageEffect : BulletEffect
{
    [System.Serializable]
    public class Params : EffectParams
    {
        [Tooltip("If > 0, overrides the damage in the hit context")] public int overrideDamage = -1;
        [Header("Damage Range")]
        [Tooltip("If true, roll damage in [min,max] instead of using base/override.")] public bool useDamageRange = false;
        [Min(0)] public int minDamage = 1;
        [Min(0)] public int maxDamage = 1;
        [Header("Criticals")] 
        [Range(0f,100f)] public float critChancePercent = 0f;
        [Min(1f)] public float critMultiplier = 1.5f;
        [Header("Floating Text")] 
        [Tooltip("Relative scale used by MMFloatingTextSpawner when IntensityImpactsScale is enabled")] public float normalScale = 1f;
        public float critScale = 1.5f;
    }

    public override EffectParams CreateDefaultParams() { return new Params(); }

    public override void Apply(ref HitContext ctx)
    {
        if (ctx.hit.collider == null) return;
        var p = _params as Params;
        int dmg = Mathf.Max(1, ctx.damage);
        if (p != null)
        {
            if (p.useDamageRange && p.maxDamage >= p.minDamage)
            {
                dmg = UnityEngine.Random.Range(p.minDamage, p.maxDamage + 1);
            }
            else if (p.overrideDamage > 0)
            {
                dmg = p.overrideDamage;
            }
        }

        bool isCrit = false;
        if (p != null && p.critChancePercent > 0f)
        {
            float roll = UnityEngine.Random.Range(0f, 100f);
            if (roll < p.critChancePercent)
            {
                isCrit = true;
                dmg = Mathf.RoundToInt(dmg * Mathf.Max(1f, p.critMultiplier));
            }
        }

        // 1) Direct parent chain
        EnemyHealth enemy = ctx.hit.collider.GetComponentInParent<EnemyHealth>();

        // 2) Proxy component on the hit collider
        if (enemy == null)
        {
            var proxy = ctx.hit.collider.GetComponent<EnemyHitboxProxy>();
            if (proxy != null) enemy = proxy.Resolve();
        }

        // 3) Tag-based fallback (if user tagged hit parts as "Enemy")
        if (enemy == null && ctx.hit.collider.CompareTag("Enemy"))
        {
            enemy = ctx.hit.collider.GetComponent<EnemyHealth>() ?? ctx.hit.collider.GetComponentInParent<EnemyHealth>();
        }

        if (enemy != null)
        {
            // Spawn floating damage text at the impact point via MMFeedbacks
            var channel = new MMChannelData(MMChannelModes.Int, isCrit ? 1 : 0, null);
            float intensity = p != null ? (isCrit ? Mathf.Max(1f, p.critScale) : Mathf.Max(0.01f, p.normalScale)) : 1f;
            // Let the spawner handle colors; we only modulate intensity/scale
            MMFloatingTextSpawnEvent.Trigger(channel, ctx.hit.point, dmg.ToString(), Vector3.up, intensity,
                false, 1f, false, null, false, null);
            enemy.TakeDamage(dmg);
        }
    }
}


