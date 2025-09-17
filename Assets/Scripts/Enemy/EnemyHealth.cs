using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using System.Collections;
using MoreMountains.Feedbacks;

public class EnemyHealth : MonoBehaviour
{
    public enum AttackType { Melee, Ranged }
    [Header("Core Stats")]
    [Tooltip("Maximum health of the enemy")] public int maxHealth = 10;
    [Tooltip("Current health of the enemy")] public int currentHealth;
    [Tooltip("Move speed used by controllers")] public float moveSpeed = 3.5f;
    [Tooltip("Attacks per second (for AI that uses it)")] public float attackSpeed = 1f;
    [Tooltip("Base contact or hit damage")] public int contactDamage = 10;
    [Range(0f,100f)] [Tooltip("Chance to critically hit the player (percent)")] public float critChancePercent = 5f;
    [Tooltip("Critical damage multiplier (e.g., 1.5 = 150%)")] public float critMultiplier = 1.5f;
    [Header("Projectile")] public int extraProjectiles = 0;

    [Header("Resistances")]
    [Range(0f,1f)]
    [Tooltip("0 = full knockback, 1 = immune. Scales down knockback applied to this enemy.")]
    public float knockbackResistance = 0f;

    [Header("Movement & Aggro")]
    [Tooltip("If true, the enemy starts aggroed")] public bool startAggro = false;
    [Tooltip("Enable gravity on the Rigidbody")] public bool useGravity = true;
    [Tooltip("Radius within which the enemy becomes aggro when the player enters")] public float aggroRange = 8f;
    [Tooltip("Prefer NavMeshAgent for pathfinding (auto-adds if missing)")] public bool preferNavMeshAgent = true;

    // Legacy contact damage / telegraphed melee settings removed

    [Header("Attack Logic")]
    public AttackType attackMode = AttackType.Melee;
    [Header("Ranged")]
    [Tooltip("Max planar distance to start ranged attacks and stop moving")] public float rangedRange = 8f;
    [Tooltip("Seconds between shots")] public float rangedCooldown = 1.0f;
    [Tooltip("Optional muzzle for ranged; if null, uses this transform")] public Transform rangedMuzzle;
    [Tooltip("Bullet profile to use for enemy ranged shots (Hitscan or Projectile)")] public BulletProfile rangedBullet;
    [Header("Melee")] 
    [Tooltip("Planar distance at which the enemy stops to perform a melee hit")] public float meleeStopRange = 2.5f;
    [Tooltip("Damage dealt to the player per successful melee hit")] public int meleeDamage = 10;
    [Tooltip("Seconds between melee hits")] public float meleeCooldown = 1.0f;
    public enum MeleeHitShape { Sphere, Box }
    [Tooltip("Collision shape used for the melee hit")] public MeleeHitShape meleeHitShape = MeleeHitShape.Sphere;
    [Tooltip("Radius of the melee hit area (sphere)")] public float meleeHitRadius = 1.2f;
    [Tooltip("Box half extents for the melee hit area (if Box shape)")] public Vector3 meleeHitBoxHalfExtents = new Vector3(0.8f, 0.8f, 0.8f);
    [Tooltip("Optional transform used as the origin for the melee hit. If null, uses this transform.")] public Transform meleeHitOrigin;
    [Tooltip("Local offset from the origin to center the melee hit (in local space of the enemy root)")] public Vector3 meleeHitLocalOffset = new Vector3(0f, 1f, 1f);
    [Tooltip("Layers considered valid targets for melee hits")] public LayerMask meleeHitMask = ~0;
    [Tooltip("Failsafe: if no Animation_AttackEnd event fires, auto-clear attack after this many seconds")] public float meleeAttackFailSafeSeconds = 1.0f;

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
    
    [Header("Death Settings")] 
    [Tooltip("Seconds to let the death animation play before starting the fade")] public float deathAnimLeadSeconds = 0.4f;
    [Tooltip("Seconds for alpha/scale fade out before destroying the enemy")] public float deathFadeSeconds = 1.2f;
    [Tooltip("Scale the corpse down during fade")] public bool deathScaleDown = true;
    [Tooltip("Target scale factor at end of fade (1 = unchanged, 0.5 = half size)")] public float deathEndScaleFactor = 0.5f;
    
    [Header("Debug Gizmos")] 
    [Tooltip("Draw melee/ranged radii when selected in the editor")] public bool drawAttackGizmos = true;
    public Color meleeHitRadiusColor = new Color(1f, 0.3f, 0.2f, 0.8f);
    public Color meleeStopRangeColor = new Color(1f, 0.8f, 0.2f, 0.8f);
    public Color rangedRangeColor = new Color(0.2f, 0.6f, 1f, 0.8f);
    
    [Header("Debug")]
    [Tooltip("If true, logs when this enemy takes damage and remaining health.")] public bool debugHits = false;
    
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

    private float _nextAttackTime = 0f;
    private bool _isAttacking = false;
    private bool _isDead = false;
    private bool _meleeSwingQueued = false;
    private float _attackFailSafeUntil = 0f;

    public float GetKnockbackMultiplier()
    {
        return 1f - Mathf.Clamp01(knockbackResistance);
    }

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
        _isAttacking = false;
    }

    void Update()
    {
        // Halt all behavior when dead
        if (_isDead) return;

        // If neither movement component exists, nothing to do
        if (rb == null && agent == null) return;

        // Ensure we have a player reference (handles player re-spawn)
        if (playerTransform == null)
        {
            var p = Object.FindFirstObjectByType<Player>();
            if (p != null) playerTransform = p.transform;
        }

        // Failsafe: if attack animation events were not wired, auto-exit attack after a short duration
        if (_isAttacking && Time.time >= _attackFailSafeUntil)
        {
            Animation_AttackEnd();
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
            // Ranged: stop when within rangedRange; Melee: keep chasing until melee range logic stops
            Vector3 toP = playerTransform.position - transform.position; toP.y = 0f;
            float planarDist = toP.magnitude;
            bool holdForRanged = (attackMode == AttackType.Ranged) && (planarDist <= Mathf.Max(0.1f, rangedRange));
            bool holdForMelee = (attackMode == AttackType.Melee) && (planarDist <= Mathf.Max(0.1f, meleeStopRange));
            bool hold = holdForRanged || holdForMelee;
            agent.isStopped = hold;
            if (!hold) agent.SetDestination(playerTransform.position);
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
            bool hold = ((attackMode == AttackType.Ranged) ? (dist <= Mathf.Max(0.1f, rangedRange)) : (attackMode == AttackType.Melee ? (dist <= Mathf.Max(0.1f, meleeStopRange)) : false)) || _isAttacking;
            // Only drive velocity when non-kinematic; otherwise, move transform directly
            if (!rb.isKinematic)
            {
                Vector3 newVel = hold ? new Vector3(0f, rb.linearVelocity.y, 0f) : new Vector3(dir.x * moveSpeed, rb.linearVelocity.y, dir.z * moveSpeed);
                rb.linearVelocity = newVel;
            }
            else
            {
                if (!hold)
                {
                    transform.position += dir * moveSpeed * Time.deltaTime;
                }
            }
            // Face the player
            if (dir != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(dir);
            }
        }

        // Attack logic
        if (attackMode == AttackType.Melee)
        {
            TryMeleeAttack();
        }
        else if (attackMode == AttackType.Ranged)
        {
            TryRangedAttack();
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
        // Stop near desired range per mode
        if (attackMode == AttackType.Ranged)
            agent.stoppingDistance = Mathf.Max(rangedRange * 0.9f, 0.25f);
        else if (attackMode == AttackType.Melee)
            agent.stoppingDistance = Mathf.Max(meleeStopRange * 0.9f, 0.25f);
        else
            agent.stoppingDistance = 0.25f;
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
        
        // Animator hit trigger
        var __bridge = GetComponentInChildren<EnemyAnimatorBridge>();
        if (__bridge != null) __bridge.FlagHit();

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
        if (debugHits)
        {
            Debug.Log($"[EnemyHealth] {name} took {damage} dmg, HP: {Mathf.Max(0, currentHealth)}/{maxHealth}");
        }
        CombatDebug.LogDamage(gameObject, damage, Mathf.Max(0, currentHealth), maxHealth);

        // Check if enemy is dead
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void OnCollisionStay(Collision collision) { }

    public void TakeReflectDamage(int damage)
    {
        // Bypass aggro kick and flash differences if desired; here we reuse the same path
        TakeDamage(damage);
    }

    private void TryApplyContactDamage() { }

    void TryTelegraphedAttack() { }

    IEnumerator DoTelegraphedAttack() { yield break; }

    void TryRangedAttack()
    {
        if (playerTransform == null) return;
        if (Time.time < _nextAttackTime) return;
        // Planar distance check
        Vector3 a = transform.position; a.y = 0f;
        Vector3 b = playerTransform.position; b.y = 0f;
        float dist = Vector3.Distance(a, b);
        if (dist > Mathf.Max(0.1f, rangedRange)) return;
        if (rangedBullet == null) return;

        // Brief attack flag for animator
        var bridge = GetComponentInChildren<EnemyAnimatorBridge>();
        if (bridge != null) bridge.SetIsAttacking(true);

        // Compute direction toward player (flattened)
        Transform muzzle = rangedMuzzle != null ? rangedMuzzle : transform;
        Vector3 dir = (playerTransform.position - muzzle.position); dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;
        dir.Normalize();

        // Fire one pellet via BulletSystem (no WeaponFireProfile effects for now)
        BulletSystem.FirePellet(transform, muzzle, dir, null, rangedBullet);
        _nextAttackTime = Time.time + Mathf.Max(0.01f, rangedCooldown);

        // Reset attack flag next frame
        if (bridge != null) bridge.SetIsAttacking(false);
    }

    void TryMeleeAttack()
    {
        if (playerTransform == null) return;
        if (Time.time < _nextAttackTime) return;
        Vector3 a = transform.position; a.y = 0f;
        Vector3 b = playerTransform.position; b.y = 0f;
        if (Vector3.Distance(a, b) > Mathf.Max(0.1f, meleeStopRange)) return;

        // Defer the actual damage to an Animation Event (Animation_MeleeStrike) at the impact frame
        _isAttacking = true;
        var bridge = GetComponentInChildren<EnemyAnimatorBridge>();
        if (bridge != null) bridge.SetIsAttacking(true);
        _nextAttackTime = Time.time + Mathf.Max(0.01f, meleeCooldown);
        _attackFailSafeUntil = Time.time + Mathf.Max(0.1f, meleeAttackFailSafeSeconds);
    }

    // Animation Event: call at the start of the attack (optional)
    public void Animation_AttackStart()
    {
        _isAttacking = true;
        var bridge = GetComponentInChildren<EnemyAnimatorBridge>();
        if (bridge != null) bridge.SetIsAttacking(true);
        _attackFailSafeUntil = Time.time + Mathf.Max(0.1f, meleeAttackFailSafeSeconds);
    }

    // Animation Event: call exactly when the strike should apply damage
    public void Animation_MeleeStrike()
    {
        if (_isDead) return;
        Transform origin = meleeHitOrigin != null ? meleeHitOrigin : transform;
        Vector3 center = origin.TransformPoint(meleeHitLocalOffset);

        Collider[] hits = null;
        if (meleeHitShape == MeleeHitShape.Sphere)
        {
            hits = Physics.OverlapSphere(center, Mathf.Max(0.01f, meleeHitRadius), meleeHitMask, QueryTriggerInteraction.Ignore);
        }
        else
        {
            Quaternion rot = origin.rotation;
            hits = Physics.OverlapBox(center, Vector3.Max(meleeHitBoxHalfExtents, new Vector3(0.01f,0.01f,0.01f)), rot, meleeHitMask, QueryTriggerInteraction.Ignore);
        }

        if (hits != null && hits.Length > 0)
        {
            for (int i = 0; i < hits.Length; i++)
            {
                var h = hits[i]; if (h == null) continue;
                var player = h.GetComponentInParent<Player>();
                if (player != null)
                {
                    player.TakeDamage(gameObject, Mathf.Max(0, meleeDamage));
                    break;
                }
            }
        }
    }

    // Animation Event: call at the end of the attack (optional)
    public void Animation_AttackEnd()
    {
        _isAttacking = false;
        var bridge = GetComponentInChildren<EnemyAnimatorBridge>();
        if (bridge != null) bridge.SetIsAttacking(false);
    }

    System.Collections.IEnumerator ClearAttackFlagNextFrame()
    {
        yield return null;
        var bridge = GetComponentInChildren<EnemyAnimatorBridge>();
        if (bridge != null) bridge.SetIsAttacking(false);
    }

    void OnDrawGizmosSelected()
    {
        if (!drawAttackGizmos) return;
        Transform origin = meleeHitOrigin != null ? meleeHitOrigin : transform;
        Vector3 worldCenter = origin.TransformPoint(meleeHitLocalOffset);
        if (attackMode == AttackType.Melee)
        {
            Gizmos.color = meleeStopRangeColor; Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.01f, meleeStopRange));
            Gizmos.color = meleeHitRadiusColor;
            if (meleeHitShape == MeleeHitShape.Sphere)
                Gizmos.DrawWireSphere(worldCenter, Mathf.Max(0.01f, meleeHitRadius));
            else
                Gizmos.DrawWireCube(worldCenter, Vector3.Max(meleeHitBoxHalfExtents * 2f, new Vector3(0.02f,0.02f,0.02f)));
        }
        else if (attackMode == AttackType.Ranged)
        {
            Gizmos.color = rangedRangeColor; Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.01f, rangedRange));
        }
    }

    // Attack indicator logic removed

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
        _isDead = true;
        isAggro = false;
        // 1) Destroy enemy UI (healthbars/nameplates)
        var uiBars = GetComponentsInChildren<UIEnemyHealthBarWorld>(true);
        for (int i = 0; i < uiBars.Length; i++) if (uiBars[i] != null) Destroy(uiBars[i].gameObject);

        // 2) Signal death animation and freeze motion/collisions
        var __bridge = GetComponentInChildren<EnemyAnimatorBridge>();
        if (__bridge != null) __bridge.SetIsDead(true);
        if (agent != null)
        {
            if (agent.enabled)
            {
                agent.ResetPath();
                agent.isStopped = true;
                agent.updatePosition = false;
                agent.updateRotation = false;
            }
            agent.velocity = Vector3.zero;
            agent.enabled = false;
        }
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
        var __cols = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < __cols.Length; i++) if (__cols[i] != null) __cols[i].enabled = false;

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
        
        // 4) Optional lead time for death anim, then fade out and destroy
        StartCoroutine(FadeAndDespawn(deathAnimLeadSeconds, deathFadeSeconds));
    }

    System.Collections.IEnumerator FadeAndDespawn(float leadSeconds, float fadeSeconds)
    {
        if (leadSeconds > 0f) yield return new WaitForSeconds(leadSeconds);
        float duration = Mathf.Max(0.01f, fadeSeconds);
        float t = 0f;
        // Cache renderers and material instances for alpha fade
        var rends = GetComponentsInChildren<Renderer>(true);
        var mats = new System.Collections.Generic.List<Material>();
        for (int i = 0; i < rends.Length; i++)
        {
            var r = rends[i]; if (r == null) continue;
            // Clone materials so we don't affect shared ones
            var arr = r.materials;
            for (int j = 0; j < arr.Length; j++)
            {
                var m = arr[j];
                mats.Add(m);
                if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f); // URP Transparent
                if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);
                if (m.HasProperty("_AlphaClip")) m.SetFloat("_AlphaClip", 0f);
            }
        }
        Vector3 startScale = transform.localScale;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(1f - (t / duration));
            for (int i = 0; i < mats.Count; i++)
            {
                var m = mats[i]; if (m == null) continue;
                if (m.HasProperty("_BaseColor"))
                {
                    var c = m.GetColor("_BaseColor"); c.a = a; m.SetColor("_BaseColor", c);
                }
                else if (m.HasProperty("_Color"))
                {
                    var c = m.GetColor("_Color"); c.a = a; m.SetColor("_Color", c);
                }
            }
            if (deathScaleDown)
            {
                float endFactor = Mathf.Clamp(deathEndScaleFactor, 0.01f, 1f);
                transform.localScale = Vector3.Lerp(startScale, startScale * endFactor, 1f - a);
            }
            yield return null;
        }
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