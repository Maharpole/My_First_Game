using UnityEngine;

[CreateAssetMenu(menuName="Combat/Bullet Profile")]
public class BulletProfile : ScriptableObject
{
    public enum Mode { Hitscan, Projectile }
    [Header("Mode")] public Mode mode = Mode.Hitscan;

    [Header("Damage")]
    public int baseDamage = 10;
    [Tooltip("Damage multiplier vs normalized distance (0 at muzzle, 1 at maxRange)")]
    public AnimationCurve damageFalloffByDistance = AnimationCurve.Linear(0, 1, 1, 1);

    [Header("Geometry & Behavior")]
    public float maxRange = 60f;
    [Min(0)] public int penetrationCount = 0;
    [Min(0)] public int ricochetCount = 0;
    public LayerMask hitMask = ~0;
    [Tooltip("If > 0, use spherecasts for hits (helps thin/fast targets)")]
    public float castRadius = 0f;
    public QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;
    [Tooltip("Random cone half-angle in degrees per pellet")]
    public float bloomDegrees = 0f;

    [Header("Projectile (if Mode=Projectile)")]
    public float speed = 40f;
    public float gravity = 0f;
    public GameObject projectilePrefab;

    [Header("VFX")]
    public GameObject tracerPrefab;
    public float tracerLifetime = 0.06f;
    public ParticleSystem impactVFX;
}


