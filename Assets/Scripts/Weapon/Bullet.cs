using UnityEngine;

public partial class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    [Tooltip("How long the bullet lives before being destroyed")]
    public float lifetime = 3f;
    
    [Tooltip("How much damage this bullet deals")]
    public float damage = 3f;
    
    [Tooltip("Whether this bullet is destroyed on impact")]
    public bool destroyOnImpact = true;

    [Header("Ground Clearance")]
    [Tooltip("Layers considered ground for keeping the projectile above the surface")]
    public LayerMask groundMask = ~0;
    [Tooltip("Minimum height to keep above ground while flying")]
    public float minGroundClearance = 0.3f;
    [Tooltip("How far above the bullet to start the downward probe each frame")]
    public float groundProbeUp = 0.5f;
    [Tooltip("How far below the bullet to probe for ground each frame")]
    public float groundProbeDown = 1.5f;

    [Header("Knockback")]
    [Header("Fast-Moving Collision (Anti-Tunneling)")]
    [Tooltip("Radius used for swept collision; match your bullet visual radius")] public float sweepRadius = 0.1f;
    [Tooltip("Layers considered hittable by the bullet")] public LayerMask hitMask = ~0;

    private Vector3 _lastPos;
    [Tooltip("Impulse force applied to rigidbody enemies along bullet travel direction")] public float knockbackForce = 0f;
    [Tooltip("Upward impulse added to knockback")] public float knockbackUp = 0f;
    [Tooltip("If enemy uses NavMeshAgent (no rigidbody), push this distance")] public float agentKnockbackDistance = 0f;
    [Tooltip("Time over which agent knockback is applied")] public float agentKnockbackTime = 0.12f;

    private void Awake()
    {
        // Set the bullet to the "Bullet" layer
        gameObject.layer = LayerMask.NameToLayer("Bullet");
    }

    protected virtual void Start()
    {
        // Destroy bullet after lifetime
        Invoke(nameof(DestroySelf), lifetime);
        
        // Add a collider if one doesn't exist
        if (GetComponent<Collider>() == null)
        {
            SphereCollider collider = gameObject.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 0.2f;
        }

        // Get the Rigidbody component
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        // Configure Rigidbody for bullet physics
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        _lastPos = transform.position;
    }

    void FixedUpdate()
    {
        // Swept collision to prevent tunneling at high speed
        Vector3 current = transform.position;
        Vector3 delta = current - _lastPos;
        float dist = delta.magnitude;
        if (dist > 0.0001f)
        {
            Ray ray = new Ray(_lastPos, delta.normalized);
            if (Physics.SphereCast(ray, Mathf.Max(0f, sweepRadius), out var sweepHit, dist, hitMask, QueryTriggerInteraction.Collide))
            {
                // Manually trigger hit
                OnTriggerEnter(sweepHit.collider);
                // Move to hit point to avoid skipping
                transform.position = sweepHit.point;
            }
        }
        _lastPos = transform.position;

        // Keep the projectile at least minGroundClearance above ground to avoid immediate hill collisions when aiming uphill
        Vector3 p = transform.position;
        Vector3 origin = p + Vector3.up * Mathf.Max(0f, groundProbeUp);
        float maxDist = groundProbeUp + groundProbeDown + minGroundClearance + 0.5f;
        if (Physics.Raycast(origin, Vector3.down, out var hit, maxDist, groundMask, QueryTriggerInteraction.Ignore))
        {
            float targetY = hit.point.y + Mathf.Max(0f, minGroundClearance);
            if (p.y < targetY)
            {
                // Lift the bullet to maintain clearance and avoid it immediately intersecting slopes
                p.y = targetY;
                transform.position = p;
                var rb = GetComponent<Rigidbody>();
                if (rb != null)
                {
                    var v = rb.linearVelocity;
                    if (v.y < 0f) { v.y = 0f; rb.linearVelocity = v; }
                }
            }
        }
    }
    
    public virtual void OnTriggerEnter(Collider other)
    {
        // Skip if the collider is the player
        if (other.CompareTag("Player")) return;

        // Check if we hit an enemy or its proxy hitbox
        EnemyHealth enemyHealth = null;
        if (other.CompareTag("Enemy"))
        {
            enemyHealth = other.GetComponent<EnemyHealth>();
            if (enemyHealth == null) enemyHealth = other.GetComponentInParent<EnemyHealth>();
        }
        else
        {
            var proxy = other.GetComponent<EnemyHitboxProxy>();
            if (proxy != null) enemyHealth = proxy.Resolve();
            if (enemyHealth == null) enemyHealth = other.GetComponentInParent<EnemyHealth>();
        }

        if (enemyHealth != null)
        {
            // Deal damage to the enemy
            enemyHealth.TakeDamage(Mathf.RoundToInt(damage));

            // Apply knockback if configured
            if (knockbackForce > 0f || agentKnockbackDistance > 0f)
            {
                Vector3 dir = GetComponent<Rigidbody>() != null && GetComponent<Rigidbody>().linearVelocity.sqrMagnitude > 0.0001f
                    ? GetComponent<Rigidbody>().linearVelocity.normalized
                    : (other.bounds.center - transform.position).normalized;

                var hitRb = other.attachedRigidbody ?? other.GetComponentInParent<Rigidbody>();
                if (hitRb != null && knockbackForce > 0f)
                {
                    Vector3 impulse = dir * knockbackForce + Vector3.up * Mathf.Max(0f, knockbackUp);
                    hitRb.AddForce(impulse, ForceMode.Impulse);
                }
                else if (agentKnockbackDistance > 0f)
                {
                    var agentGo = other.GetComponentInParent<UnityEngine.AI.NavMeshAgent>()?.gameObject;
                    if (agentGo != null)
                    {
                        StartCoroutine(KnockbackAgent(agentGo.transform, dir, agentKnockbackDistance, agentKnockbackTime));
                    }
                }
            }
            
            // Destroy the bullet if configured to do so
            if (destroyOnImpact)
            {
                DestroySelf();
            }
        }
    }

    System.Collections.IEnumerator KnockbackAgent(Transform target, Vector3 dir, float distance, float time)
    {
        if (target == null) yield break;
        var agent = target.GetComponent<UnityEngine.AI.NavMeshAgent>();
        bool hadAgent = agent != null && agent.enabled;
        if (hadAgent) agent.enabled = false;
        Vector3 start = target.position;
        Vector3 end = start + new Vector3(dir.x, 0f, dir.z).normalized * Mathf.Max(0f, distance);
        float t = 0f;
        while (t < 1f)
        {
            t += (time > 0f ? Time.deltaTime / time : 1f);
            target.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)));
            yield return null;
        }
        if (hadAgent && target != null)
        {
            agent.enabled = true;
        }
    }
} 

public partial class Bullet : MonoBehaviour
{
    void DestroySelf()
    {
        var trail = GetComponent<BulletTrailHandler>();
        if (trail != null) trail.DetachTrailNow();
        Destroy(gameObject);
    }
}