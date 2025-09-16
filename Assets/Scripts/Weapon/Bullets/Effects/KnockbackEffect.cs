using UnityEngine;

[CreateAssetMenu(menuName="Combat/Effects/Knockback")]
public class KnockbackEffect : BulletEffect
{
    [Header("Force")]
    public float force = 5f;
    public float upForce = 0f;
    public ForceMode mode = ForceMode.Impulse;

    [Header("Options")]
    [Tooltip("Multiply horizontal force by the hit context damage value.")]
    public bool scaleByDamage = false;
    [Tooltip("If true, use the surface normal instead of shot direction for push.")]
    public bool pushAlongSurfaceNormal = false;

    public override void Apply(ref HitContext ctx)
    {
        if (ctx.hit.collider == null) return;
        var rb = ctx.hit.rigidbody;
        if (rb == null) rb = ctx.hit.collider.GetComponentInParent<Rigidbody>();
        if (rb == null || rb.isKinematic) return;

        Vector3 dir = pushAlongSurfaceNormal && ctx.hit.normal != Vector3.zero
            ? ctx.hit.normal
            : (ctx.direction.sqrMagnitude > 0.0001f ? ctx.direction : (ctx.hit.point - ctx.origin).normalized);

        float magnitude = force * (scaleByDamage ? Mathf.Max(1f, ctx.damage) : 1f);
        Vector3 impulse = dir * magnitude + Vector3.up * Mathf.Max(0f, upForce);
        rb.AddForce(impulse, mode);
    }
}




