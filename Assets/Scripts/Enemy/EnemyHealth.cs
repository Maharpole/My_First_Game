using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    [Header("Core Stats")]
    [Tooltip("Maximum health of the enemy")] public int maxHealth = 10;
    [Tooltip("Current health of the enemy")] public int currentHealth;
    [Tooltip("Move speed used by controllers")] public float moveSpeed = 3.5f;
    [Tooltip("Attacks per second (for AI that uses it)")] public float attackSpeed = 1f;
    [Tooltip("Base contact or hit damage")] public int contactDamage = 10;
    [Range(0f,100f)] [Tooltip("Chance to critically hit the player (percent)")] public float critChancePercent = 5f;
    [Tooltip("Critical damage multiplier (e.g., 1.5 = 150%)")] public float critMultiplier = 1.5f;
    [Header("Projectile")] public int extraProjectiles = 0;

    [Header("Movement & Aggro")]
    [Tooltip("If true, the enemy starts aggroed")] public bool startAggro = false;
    [Tooltip("Enable gravity on the Rigidbody")] public bool useGravity = true;
    [Tooltip("Radius within which the enemy becomes aggro when the player enters")] public float aggroRange = 8f;
    [Tooltip("Prefer NavMeshAgent for pathfinding (auto-adds if missing)")] public bool preferNavMeshAgent = true;

    [Header("Contact Damage (Legacy)")]
    [Tooltip("Meters from enemy center to apply touch damage to the player")] public float contactRange = 0.8f;
    [Tooltip("Seconds between consecutive contact damage ticks")] public float contactDamageInterval = 0.5f;

    [Header("Telegraphed Melee Attack")]
    public bool useTelegraphedAttack = true;
    [Tooltip("Begin attack when within this distance to the player (planar)")] public float attackRange = 2.5f;
    [Tooltip("Radius of the red warning circle and damage area")] public float attackRadius = 1.8f;
    [Tooltip("Seconds to show warning before striking")] public float attackWindup = 0.5f;
    [Tooltip("Cooldown after striking before next attempt")] public float attackCooldown = 1.2f;
    [Tooltip("Optional prefab for the red warning (will be scaled to radius). If null, a simple cylinder is created.")]
    public GameObject telegraphPrefab;
    public Color telegraphColor = new Color(1f, 0f, 0f, 0.35f);
    [Tooltip("Material for telegraph quad (transparent). If null, a basic URP Unlit material will be created at runtime.")]
    public Material telegraphMaterial;
    [Tooltip("Lift above ground to avoid clipping on uneven terrain")] public float telegraphGroundOffset = 0.05f;
    [Tooltip("Layers considered ground for placing the hit indicator")] public LayerMask telegraphGroundMask = ~0;

    [Header("Leveling")]
    [Tooltip("Apply level-based max health at start unless overridden by spawner")] public bool useLevelScaling = true;
    [Min(1)] public int level = 1;
    [Tooltip("Health gained per level above 1")] public int healthPerLevel = 5;
    [Tooltip("Multiplier per level (applied after flat)")] public float healthLevelMultiplier = 1.0f;

    [Header("Experience Reward")] public int baseXP = 5;
    [Tooltip("XP gained per level above 1")] public int xpPerLevel = 2;

    [Header("Visual Effects")]
    [Tooltip("Duration of the damage flash effect")]
    public float flashDuration = 0.1f;
    
    [Tooltip("Color to flash when taking damage")]
    public Color damageColor = Color.red;
    
    [Tooltip("Color to flash when healing")]
    public Color healColor = Color.green;
    
    [Tooltip("Particle system to play on death")]
    public ParticleSystem deathParticles;
    [Tooltip("Particle system to play when damaged (instanced)")] public ParticleSystem damageHitParticles;
    [Tooltip("Damage hit sounds; one will be chosen at random on damage")] public AudioClip[] damageHitClips;
    [Range(0f,1f)] public float damageHitSFXVolume = 1f;
    [Tooltip("Random pitch range for damage sounds")] public Vector2 damageHitPitchRange = new Vector2(1f,1f);
    
    private Material originalMaterial;
    private Renderer enemyRenderer;
    private Color originalColor;

    // Movement state
    private Transform playerTransform;
    private Rigidbody rb;
    private NavMeshAgent agent;
    private bool usingAgent = false;
    // Cache the core max health (from Core Stats) so level scaling uses that as the base
    private int coreBaseMaxHealth;
    private float lastAppliedMoveSpeed = -1f;
    private bool isAggro = false;
    [HideInInspector] public bool ignoreLevelScaling = false; // set true by spawner if it overrides maxHealth
    
    [Header("Events")] public UnityEvent onDeath; public UnityEvent onDamage; public UnityEvent onHeal;

    private float _lastContactDamageTime = -999f;
    private float _nextAttackTime = 0f;
    private Coroutine _attackRoutine;
    private GameObject _activeTelegraph;
    private bool _isAttacking = false;

    void Start()
    {
        // Capture the Core Stats maxHealth as the base for level scaling
        coreBaseMaxHealth = Mathf.Max(1, maxHealth);
        // Apply level scaling unless a spawner told us to ignore it
        if (useLevelScaling && !ignoreLevelScaling)
        {
            ApplyLevelToHealth();
        }
        // Initialize current health from max
        currentHealth = maxHealth;
        
        // Cache player reference
        var playerObj = Object.FindFirstObjectByType<Player>();
        if (playerObj != null) playerTransform = playerObj.transform;

        // Movement components: prefer NavMeshAgent if present & enabled; otherwise, use Rigidbody
        agent = GetComponent<NavMeshAgent>();
        if (preferNavMeshAgent && (agent == null))
        {
            agent = gameObject.AddComponent<NavMeshAgent>();
            agent.enabled = true;
        }
        usingAgent = agent != null && agent.enabled;
        // Ensure the agent is actually on a NavMesh; otherwise fall back to RB
        if (usingAgent)
        {
            if (NavMesh.SamplePosition(transform.position, out var hit, 2f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
            else
            {
                agent.enabled = false;
                usingAgent = false;
            }
        }

        if (usingAgent)
        {
            // Configure agent-based movement
            ConfigureAgentMove();

            // Ensure there is a Rigidbody but make it kinematic to avoid physics interference
            rb = GetComponent<Rigidbody>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        else
        {
            // Ensure physics components for RB-driven motion
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            rb.isKinematic = false;
            rb.freezeRotation = true;
            rb.useGravity = useGravity;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.linearDamping = 0f;
        }

        if (GetComponent<Collider>() == null)
        {
            CapsuleCollider capsule = gameObject.AddComponent<CapsuleCollider>();
            capsule.height = 2.0f;
            capsule.radius = 0.5f;
            capsule.center = new Vector3(0, 1.0f, 0);
        }

        isAggro = startAggro;

        // Get the renderer component
        enemyRenderer = GetComponent<Renderer>();
        if (enemyRenderer != null)
        {
            // Store the original material and color
            originalMaterial = enemyRenderer.material;
            originalColor = enemyRenderer.material.color;
        }
        
        // Check for death particles
        if (deathParticles == null)
        {
            deathParticles = GetComponentInChildren<ParticleSystem>();
        }

        // Ensure root motion does not fight controller-driven movement
        var anim = GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.applyRootMotion = false;
        }
    }

    void OnDisable()
    {
        if (_activeTelegraph != null) { Destroy(_activeTelegraph); _activeTelegraph = null; }
        if (_attackRoutine != null) { StopCoroutine(_attackRoutine); _attackRoutine = null; }
        _isAttacking = false;
    }

    void Update()
    {
        // If neither movement component exists, nothing to do
        if (rb == null && agent == null) return;

        // Ensure we have a player reference (handles player re-spawn)
        if (playerTransform == null)
        {
            var p = Object.FindFirstObjectByType<Player>();
            if (p != null) playerTransform = p.transform;
        }

        // Auto-aggro when within range
        if (!isAggro && playerTransform != null)
        {
            Vector3 d = playerTransform.position - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude <= aggroRange * aggroRange)
            {
                ForceAggro(playerTransform);
            }
        }

        // Simple chase movement when aggroed (disabled while attacking)
        if (!isAggro || playerTransform == null) return;

        Vector3 toPlayer = playerTransform.position - transform.position;
        toPlayer.y = 0f;
        Vector3 dir = toPlayer.sqrMagnitude > 0.0001f ? toPlayer.normalized : Vector3.zero;

        if (_isAttacking)
        {
            // Hold position during attack windup/strike
            if (usingAgent && agent != null && agent.enabled)
            {
                agent.isStopped = true;
            }
            else if (rb != null)
            {
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            }
            // Face the player while attacking
            if (dir != Vector3.zero) transform.rotation = Quaternion.LookRotation(dir);
        }
        else if (usingAgent && agent != null && agent.enabled)
        {
            // Update agent settings if speed changed so runtime moveSpeed takes effect quickly
            if (!Mathf.Approximately(lastAppliedMoveSpeed, moveSpeed))
            {
                ConfigureAgentMove();
            }
            // Set destination toward the player; agent handles pathing
            agent.isStopped = false;
            agent.SetDestination(playerTransform.position);
            // Manual facing toward desired direction
            if (dir != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(dir);
            }
        }
        else if (rb != null)
        {
            // RB-driven movement
            // Use linearVelocity if available; maintain Y component for gravity
            float dist = Vector3.Distance(transform.position, playerTransform.position);
            bool hold = dist <= Mathf.Max(0.1f, attackRange * 0.9f) || _isAttacking;
            Vector3 newVel = hold ? new Vector3(0f, rb.linearVelocity.y, 0f) : new Vector3(dir.x * moveSpeed, rb.linearVelocity.y, dir.z * moveSpeed);
            rb.linearVelocity = newVel;
            // Face the player
            if (dir != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(dir);
            }
        }

        // Telegraphed attack logic replaces contact damage
        if (useTelegraphedAttack)
        {
            TryTelegraphedAttack();
        }
        else
        {
            TryApplyContactDamage();
        }
    }

    private void ConfigureAgentMove()
    {
        if (agent == null) return;
        lastAppliedMoveSpeed = moveSpeed;
        agent.speed = Mathf.Max(0f, moveSpeed);
        // High acceleration helps large speeds feel responsive
        agent.acceleration = Mathf.Max(10f, moveSpeed * 10f);
        // Avoid sliding past stop; enable braking
        agent.autoBraking = true;
        agent.autoRepath = true;
        // We handle rotation manually
        agent.updateRotation = false;
        agent.updatePosition = true;
        // Stop near attack range so enemies don't overlap the player
        agent.stoppingDistance = Mathf.Max(attackRange * 0.9f, 0.25f);
        // Ensure agent radius/height roughly match collider for better avoidance
        var col = GetComponent<Collider>() as CapsuleCollider;
        if (col != null)
        {
            agent.radius = Mathf.Max(0.1f, col.radius);
            agent.height = Mathf.Max(0.5f, col.height);
            // Base offset 0 assumes model pivot is near feet. Adjust in prefab if needed.
            agent.baseOffset = 0f;
        }
        // Enable obstacle avoidance
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        agent.avoidancePriority = Mathf.Clamp(50, 0, 99);
    }
    
    public void TakeDamage(int damage)
    {
        // Reduce health
        currentHealth -= damage;
        
        // Trigger damage event
        onDamage.Invoke();
        // Notify pack aggro if present
        var group = GetComponentInParent<AggroGroup>();
        if (group != null)
        {
            group.OnMemberDamaged();
        }
        else
        {
            // Fallback: if no group, aggro this enemy toward the player
            var player = Object.FindFirstObjectByType<Player>();
            if (player != null)
            {
                ForceAggro(player.transform);
            }
        }
        
        // Flash red
        StartCoroutine(FlashColor(damageColor));
        // Damage particles
        if (damageHitParticles != null)
        {
            var ps = Instantiate(damageHitParticles, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            var main = ps.main; main.simulationSpace = ParticleSystemSimulationSpace.World; main.scalingMode = ParticleSystemScalingMode.Local; main.simulationSpeed = 1f;
            ps.Play();
            Destroy(ps.gameObject, ps.main.duration);
        }
        // Damage SFX
        if (damageHitClips != null && damageHitClips.Length > 0)
        {
            var src = GetComponent<AudioSource>();
            if (src == null) src = gameObject.AddComponent<AudioSource>();
            var clip = damageHitClips[Random.Range(0, damageHitClips.Length)];
            if (clip != null)
            {
                float minP = Mathf.Min(damageHitPitchRange.x, damageHitPitchRange.y);
                float maxP = Mathf.Max(damageHitPitchRange.x, damageHitPitchRange.y);
                src.pitch = Random.Range(minP, maxP);
                src.PlayOneShot(clip, damageHitSFXVolume);
            }
        }
        
        // Debug logging for tuning
        CombatDebug.LogDamage(gameObject, damage, Mathf.Max(0, currentHealth), maxHealth);

        // Check if enemy is dead
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (!useTelegraphedAttack)
        {
            var player = collision.gameObject.GetComponentInParent<Player>();
            if (player == null) return;
            int dmg = Mathf.Max(0, contactDamage);
            if (dmg <= 0) return;
            // Simple crit roll
            if (Random.value < Mathf.Clamp01(critChancePercent * 0.01f))
            {
                dmg = Mathf.RoundToInt(dmg * Mathf.Max(1f, critMultiplier));
            }
            player.TakeDamage(gameObject, dmg);
        }
    }

    public void TakeReflectDamage(int damage)
    {
        // Bypass aggro kick and flash differences if desired; here we reuse the same path
        TakeDamage(damage);
    }

    private void TryApplyContactDamage()
    {
        if (playerTransform == null) return;
        if (contactDamage <= 0) return;
        if (Time.time < _lastContactDamageTime + Mathf.Max(0.01f, contactDamageInterval)) return;

        Vector3 a = transform.position;
        Vector3 b = playerTransform.position;
        a.y = 0f; b.y = 0f;
        float dist = Vector3.Distance(a, b);
        if (dist <= Mathf.Max(0.05f, contactRange))
        {
            var player = playerTransform.GetComponent<Player>();
            if (player != null)
            {
                int dmg = Mathf.Max(0, contactDamage);
                if (dmg > 0)
                {
                    // Simple crit roll to mirror OnCollisionStay behavior
                    if (Random.value < Mathf.Clamp01(critChancePercent * 0.01f))
                    {
                        dmg = Mathf.RoundToInt(dmg * Mathf.Max(1f, critMultiplier));
                    }
                    player.TakeDamage(gameObject, dmg);
                    _lastContactDamageTime = Time.time;
                }
            }
        }
    }

    void TryTelegraphedAttack()
    {
        if (playerTransform == null) return;
        if (Time.time < _nextAttackTime) return;
        // Distance on XZ plane
        Vector3 a = transform.position; a.y = 0f;
        Vector3 b = playerTransform.position; b.y = 0f;
        float dist = Vector3.Distance(a, b);
        if (dist > Mathf.Max(0.1f, attackRange)) return;
        if (_attackRoutine == null && !_isAttacking)
        {
            _attackRoutine = StartCoroutine(DoTelegraphedAttack());
        }
    }

    IEnumerator DoTelegraphedAttack()
    {
        _isAttacking = true;
        // Clean up any stale telegraph from prior cycles
        if (_activeTelegraph != null) { Destroy(_activeTelegraph); _activeTelegraph = null; }
        // Notify animator bridge
        var bridge = GetComponentInChildren<EnemyAnimatorBridge>();
        if (bridge != null) bridge.SetIsAttacking(true);
        // Stop moving during windup/strike
        if (usingAgent && agent != null && agent.enabled)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }
        else if (rb != null)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }

        // Place telegraph at player's current ground position
        Vector3 center = playerTransform.position;
        Vector3 groundNormal = Vector3.up;
        if (Physics.Raycast(center + Vector3.up * 50f, Vector3.down, out var gHit, 200f, telegraphGroundMask, QueryTriggerInteraction.Ignore))
        {
            center = gHit.point + telegraphGroundOffset * Vector3.up;
            groundNormal = gHit.normal.sqrMagnitude > 0.0001f ? gHit.normal : Vector3.up;
        }
        Vector3 faceDir = playerTransform != null ? (playerTransform.position - transform.position) : transform.forward;
        faceDir.y = 0f; if (faceDir.sqrMagnitude > 0.0001f) faceDir.Normalize(); else faceDir = transform.forward;
        var tele = SpawnTelegraph(center, attackRadius, faceDir);
        _activeTelegraph = tele;
        var hi = tele != null ? tele.GetComponent<HitIndicator>() : null;
        // Animate inner progress during windup
        float t = 0f, dur = Mathf.Max(0.0001f, attackWindup);
        while (t < dur)
        {
            t += Time.deltaTime;
            if (hi != null) hi.SetProgress(Mathf.Clamp01(t / dur));
            yield return null;
        }

        // Strike: damage if player currently inside radius
        if (playerTransform != null)
        {
            Vector3 p = playerTransform.position; p.y = center.y;
            if (Vector3.Distance(p, center) <= attackRadius + 0.01f)
            {
                var player = playerTransform.GetComponent<Player>();
                if (player != null)
                {
                    int dmg = Mathf.Max(0, contactDamage);
                    if (Random.value < Mathf.Clamp01(critChancePercent * 0.01f))
                    {
                        dmg = Mathf.RoundToInt(dmg * Mathf.Max(1f, critMultiplier));
                    }
                    player.TakeDamage(gameObject, dmg);
                }
            }
        }

        _nextAttackTime = Time.time + Mathf.Max(0.01f, attackCooldown);
        if (_activeTelegraph != null) { Destroy(_activeTelegraph); _activeTelegraph = null; }
        _attackRoutine = null;
        _isAttacking = false;
        if (bridge != null) bridge.SetIsAttacking(false);
    }

    GameObject SpawnTelegraph(Vector3 center, float radius, Vector3 faceDir)
    {
        try
        {
            GameObject go;
            if (telegraphPrefab != null)
            {
                go = Instantiate(telegraphPrefab, center, Quaternion.Euler(90f, 0f, 0f));
            }
            else
            {
                // Create a flat quad "projection" a little above ground
                go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                var col = go.GetComponent<Collider>(); if (col != null) Destroy(col);
                go.transform.position = center + telegraphGroundOffset * Vector3.up;
                // Lay flat, rotate so +X points to enemy forward direction in XZ
                float yaw = Mathf.Atan2(faceDir.z, faceDir.x) * Mathf.Rad2Deg; // align quad local +X to world forward
                go.transform.rotation = Quaternion.Euler(90f, -yaw, 0f);
                var mr = go.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    mr.receiveShadows = false;
                    if (telegraphMaterial != null)
                    {
                        mr.material = telegraphMaterial;
                        if (mr.material.HasProperty("_Color")) mr.material.color = telegraphColor;
                    }
                    else
                    {
                        var mat = new Material(Shader.Find("Game/HitIndicatorSector"));
                        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", telegraphColor);
                        mr.material = mat;
                    }
                    // Configure sector shader
                    var hb = go.AddComponent<HitIndicator>();
                    hb.meshRenderer = mr;
                    hb.Configure(radius, /*angle*/ 120f, /*rotation*/ 0f, /*progress*/ 0f, telegraphColor, new Color(1f,0.3f,0.3f,0.6f));
                }
            }
            // Quad default size is 1x1 in local XY; after rotation (90,0,0) XZ => scale evenly by diameter
            go.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
            return go;
        }
        catch { return null; }
    }

    public void Heal(int amount)
    {
        // Increase health, but don't exceed max health
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        
        // Trigger heal event
        onHeal.Invoke();
        
        // Flash green
        StartCoroutine(FlashColor(healColor));
    }
    
    IEnumerator FlashColor(Color flashColor)
    {
        if (enemyRenderer != null)
        {
            // Change to flash color
            enemyRenderer.material.color = flashColor;
            
            // Wait for flash duration
            yield return new WaitForSeconds(flashDuration);
            
            // Return to original color
            enemyRenderer.material.color = originalColor;
        }
    }
    
    void Die()
    {
        // Trigger death event (drops, etc.)
        onDeath.Invoke();
        // Award XP based on level settings
        var xpTarget = Object.FindFirstObjectByType<PlayerXP>();
        if (xpTarget != null)
        {
            int clampedLevel = Mathf.Max(1, level);
            int reward = Mathf.Max(0, baseXP + (clampedLevel - 1) * xpPerLevel);
            xpTarget.AddXP(reward);
        }
        // Also notify aggro group to avoid lingering idle
        var group = GetComponentInParent<AggroGroup>();
        if (group != null)
        {
            group.OnMemberDamaged();
        }
        
        // Play death particles if available
        if (deathParticles != null)
        {
            ParticleSystem psInstance = deathParticles;
            bool isSceneObject = psInstance.gameObject.scene.IsValid();
            bool isChildOfThis = psInstance.transform.IsChildOf(transform);

            // If the assigned particle is a prefab asset or not a child of this enemy, instantiate a runtime copy
            if (!isSceneObject || !isChildOfThis)
            {
                psInstance = Instantiate(deathParticles, transform.position, Quaternion.identity);
            }
            else
            {
                // Detach child particle system to let it play after enemy is destroyed
                psInstance.transform.parent = null;
                psInstance.transform.position = transform.position;
                psInstance.transform.rotation = Quaternion.identity;
            }

            // Normalize transform so parent scale/motion doesn't speed things up
            psInstance.transform.localScale = Vector3.one;

            // Force world-space, local scaling mode, and sane speeds regardless of prefab settings
            var main = psInstance.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Local;
            main.simulationSpeed = 1f;

            var inherit = psInstance.inheritVelocity;
            inherit.enabled = false;

            // Optional: disable velocity over lifetime to avoid unexpected bursts
            var vol = psInstance.velocityOverLifetime;
            if (vol.enabled)
            {
                vol.enabled = false;
            }

            // Play and clean up after duration
            psInstance.Play();
            Destroy(psInstance.gameObject, psInstance.main.duration);
        }
        
        // Destroy the enemy next frame to let listeners complete without blocking this frame
        Destroy(gameObject);
    }
    
    void OnDestroy()
    {
        // Clean up the material if we created a new one
        if (originalMaterial != null)
        {
            Destroy(originalMaterial);
        }
    }

    public void ForceAggro(Transform target)
    {
        playerTransform = target;
        isAggro = true;
    }

    public void ClearAggro()
    {
        isAggro = false;
        if (usingAgent && agent != null && agent.enabled)
        {
            agent.ResetPath();
        }
        else if (rb != null)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }
    }

    public void ApplyLevelToHealth()
    {
        int clampedLevel = Mathf.Max(1, level);
        int flat = Mathf.Max(1, coreBaseMaxHealth) + (clampedLevel - 1) * Mathf.Max(0, healthPerLevel);
        float scaled = flat * Mathf.Max(0.1f, Mathf.Pow(Mathf.Max(0.0001f, healthLevelMultiplier), clampedLevel - 1));
        maxHealth = Mathf.Max(1, Mathf.RoundToInt(scaled));
        currentHealth = maxHealth;
    }
} 