using UnityEngine;

[CreateAssetMenu(menuName="Combat/Effects/Damage")]
public class DamageEffect : BulletEffect
{
    [Tooltip("If > 0, overrides the damage in the hit context")] public int overrideDamage = -1;

    public override void Apply(ref HitContext ctx)
    {
        if (ctx.hit.collider == null) return;
        int dmg = overrideDamage > 0 ? overrideDamage : Mathf.Max(1, ctx.damage);

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
            enemy.TakeDamage(dmg);
        }
    }
}


