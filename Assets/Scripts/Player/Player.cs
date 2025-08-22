using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DefaultExecutionOrder(-200)]
/// <summary>
/// Single, unified player script that handles everything player-related.
/// No more multiple scripts - this is the ONLY script you need on your player.
/// </summary>
public class Player : MonoBehaviour
{
    public static Player Instance { get; private set; }
    
    [Header("== MOVEMENT ==")]
    public float moveSpeed = 5f;
    [Tooltip("Degrees per second to rotate towards movement direction")] public float rotationSpeed = 720f;
    [Tooltip("Rotate to face the direction the player is moving")] public bool faceMoveDirection = true;
    public float dashSpeed = 20f;
    public float dashDuration = 0.2f;
    public int maxDashCharges = 3;
    public float dashRechargeTime = 5f;
    [Header("== GROUNDING ==")]
    public bool projectToGround = true;
    public LayerMask groundMask;
    public float groundRayHeight = 2f;
    public float groundOffset = 0.05f;
    
    [Header("== HEALTH & COMBAT ==")]
    public int maxHealth = 100;
    [SerializeField] private int currentHealth = 100;
    public float damageCooldown = 1f;
    [HideInInspector] public int contactDamage = 0; // deprecated
    
    [Header("== CURRENCY ==")]
    [SerializeField] private int coins = 0;
    
    // Equipment is managed by CharacterEquipment component on the same GameObject.
    
    [Header("== VISUAL EFFECTS ==")]
    public Color damageColor = Color.red;
    public float flashDuration = 0.2f;
    public int flashCount = 3;
    public ParticleSystem dashParticles;
    
    [Header("== AUDIO ==")]
    public AudioClip[] dashSounds;
    public AudioClip damageSound;
    public AudioClip deathSound;
    [Range(0f, 1f)] public float dashVolume = 1f;
    
    [Header("== EVENTS ==")]
    public UnityEvent<int> onHealthChanged;
    public UnityEvent<int> onCoinsChanged;
    public UnityEvent onPlayerDeath;
    public UnityEvent onEquipmentChanged;
    public UnityEvent<int> onDamaged;
    
    // Private fields
    private bool isDashing = false;
    private float dashTime = 0f;
    private Vector3 dashDirection = Vector3.zero;
    private int currentDashCharges;
    private float rechargeTimer = 0f;
    private Dictionary<GameObject, float> damageCooldowns = new Dictionary<GameObject, float>();
    private Renderer playerRenderer;
    private Color originalColor;
    private AudioSource audioSource;
    private bool isFlashing = false;
    private CharacterEquipment characterEquipment;
    private PlayerAnimatorBridge _animBridge;
#if ENABLE_INPUT_SYSTEM
    private Input_Control _input;
#endif
        private float baseMoveSpeed;
        private int baseMaxHealth;
        private float reflectFlatCurrent = 0f;
        private float reflectPercentCurrent = 0f;
        private int regenPerTickCurrent = 0;
        private float regenTickSecondsCurrent = 1f;
        public bool enableStatLogs = true;
        StatsSnapshot _lastStats;

        struct StatsSnapshot
        {
            public float baseMove, flatMove, pctMove;
            public int baseHp, flatHp, bonusPctHp, maxHp, curHp;
            public float dmgFlat, dmgPct, atkSpdPct;
            public override string ToString()
            {
                float finalMove = (baseMove + flatMove) * (1f + pctMove / 100f);
                return $"Move: base={baseMove} +{flatMove} {pctMove}% -> {finalMove:0.###}\n" +
                       $"HP: base={baseHp} +{flatHp} +{bonusPctHp} => max={maxHp}, cur={curHp}\n" +
                       $"Dmg: +{dmgFlat} {dmgPct}%  AS%={atkSpdPct}";
            }
        }

        void LogStats(
                float baseMove, float flatMove, float pctMove,
                int baseHp, int flatHp, int bonusPctHp, int maxHp, int curHp,
                float dmgFlat, float dmgPct, float atkSpdPct,
                string reason)
        {
            if (!enableStatLogs) return;
            _lastStats = new StatsSnapshot
            {
                baseMove = baseMove, flatMove = flatMove, pctMove = pctMove,
                baseHp = baseHp, flatHp = flatHp, bonusPctHp = bonusPctHp, maxHp = maxHp, curHp = curHp,
                dmgFlat = dmgFlat, dmgPct = dmgPct, atkSpdPct = atkSpdPct
            };
            Debug.Log($"[Stats] {reason}\n{_lastStats}");
        }
    
    // Properties
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public int Coins => coins;
    public bool IsAlive => currentHealth > 0;
    
    void Awake()
    {
        // Singleton for easy access
        if (Instance == null)
        {
            Instance = this;
            // Persist the player across scene loads (must be on a root GameObject)
            var root = transform.root != null ? transform.root.gameObject : gameObject;
            if (root.transform.parent == null)
            {
                DontDestroyOnLoad(root);
            }
        }
        else
        {
            Debug.LogWarning("Multiple Player instances found! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        
        InitializePlayer();
    }
    
    void InitializePlayer()
    {
        // Initialize movement
        currentDashCharges = maxDashCharges;
        if (moveSpeed <= 0f) moveSpeed = 5f; // guard against zeroed prefab values
        baseMoveSpeed = moveSpeed; // remember unmodified baseline for stat calculations
        
        // Initialize health
        if (maxHealth <= 0) maxHealth = 100; // guard against zeroed prefab values
        baseMaxHealth = maxHealth; // remember unmodified baseline for max health
        currentHealth = maxHealth;
        
        // Hook up character equipment events
        characterEquipment = GetComponent<CharacterEquipment>();
        if (characterEquipment != null)
        {
            characterEquipment.onEquipmentChanged.AddListener(HandleEquipmentChanged);
        }
        
        // Initialize components
        playerRenderer = GetComponent<Renderer>();
        if (playerRenderer != null)
        {
            originalColor = playerRenderer.material.color;
        }
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Cache animator bridge for triggering dash animations
        _animBridge = GetComponent<PlayerAnimatorBridge>();
        if (_animBridge == null) _animBridge = GetComponentInChildren<PlayerAnimatorBridge>();
        if (_animBridge == null) _animBridge = GetComponentInParent<PlayerAnimatorBridge>();
        if (_animBridge == null)
        {
            Debug.LogWarning("[Player] PlayerAnimatorBridge not found on Player object or hierarchy; dash animation will not be triggered.");
        }
        
        // Initialize particle emission
        if (dashParticles != null)
        {
            var emission = dashParticles.emission;
            emission.enabled = false;
        }
        
        // Apply initial stats from any pre-equipped gear
        RecomputeAndApplyStats();
        LogStats(baseMoveSpeed, 0f, 0f, baseMaxHealth, 0, 0, maxHealth, currentHealth, 0f, 0f, 0f, "Initialize");
        Debug.Log("Player initialized successfully!");

        // Apply starting class to player (can be used to seed skill tree later)
        var startingClass = PlayerProfile.StartingClass;
        Debug.Log($"StartingClass: {startingClass}");
    }
    
#if ENABLE_INPUT_SYSTEM
    void OnEnable()
    {
        if (_input == null)
        {
            _input = new Input_Control();
        }
        _input.Enable();
        // Dash via new input system
        _input.Gameplay.Dash.performed += OnDashAction;
    }

    void OnDisable()
    {
        if (_input != null)
        {
            _input.Gameplay.Dash.performed -= OnDashAction;
            _input.Disable();
        }
    }

    void OnDashAction(InputAction.CallbackContext _)
    {
        if (isDashing || currentDashCharges <= 0) return;
        Vector3 dir = GetMoveVector();
        if (dir.sqrMagnitude < 0.01f) dir = new Vector3(transform.forward.x, 0f, transform.forward.z);
        if (dir.sqrMagnitude > 0.0001f)
        {
            PerformDash(dir);
        }
    }
#endif
    
    void Update()
    {
        HandleMovement();
        if (projectToGround)
        {
            ProjectDownToGround();
        }
        UpdateDashCharges();
        UpdateDamageCooldowns();
    }
    
    #region MOVEMENT
    void HandleMovement()
    {
        if (isDashing)
        {
            // Move in dash direction every frame during dash
            if (dashDirection.sqrMagnitude > 0.0001f)
            {
                transform.Translate(dashDirection * dashSpeed * Time.deltaTime, Space.World);
            }
            dashTime += Time.deltaTime;
            if (dashTime >= dashDuration)
            {
                isDashing = false;
                dashDirection = Vector3.zero;
                Debug.Log("Dash END");
                // Stop dash particles after dash ends
                if (dashParticles != null)
                {
                    var emission = dashParticles.emission;
                    emission.enabled = false;
                    dashParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
                // Signal dash end to animator
                if (_animBridge != null) _animBridge.FlagDashEnded();
            }
            return;
        }
        
        // Get input
        Vector3 movement = GetMoveVector();
        
        // Move the player
        if (movement.magnitude > 0.1f)
        {
            transform.Translate(movement * moveSpeed * Time.deltaTime, Space.World);

            // Face movement direction
            if (faceMoveDirection)
            {
                Vector3 look = new Vector3(movement.x, 0f, movement.z);
                if (look.sqrMagnitude > 0.0001f)
                {
                    Quaternion target = Quaternion.LookRotation(look, Vector3.up);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, target, rotationSpeed * Time.deltaTime);
                }
            }
        }
        
        // Handle dash
        // Old Input fallback only when new input system is not enabled
#if !ENABLE_INPUT_SYSTEM
        if (Input.GetKeyDown(KeyCode.Space) && currentDashCharges > 0 && movement.magnitude > 0.1f)
        {
            PerformDash(movement);
        }
#endif
    }

    Vector3 GetMoveVector()
    {
#if ENABLE_INPUT_SYSTEM
        // Derive WASD from Input System keyboard state
        var kb = Keyboard.current;
        if (kb != null)
        {
            float x = (kb.dKey.isPressed ? 1f : 0f) + (kb.aKey.isPressed ? -1f : 0f);
            float z = (kb.wKey.isPressed ? 1f : 0f) + (kb.sKey.isPressed ? -1f : 0f);
            Vector3 v = new Vector3(x, 0f, z);
            if (v.sqrMagnitude > 1f) v.Normalize();
            return v;
        }
#endif
        // Legacy Input Manager fallback
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        return new Vector3(horizontal, 0f, vertical).normalized;
    }
    
    void PerformDash(Vector3 direction)
    {
        isDashing = true;
        dashTime = 0f;
        currentDashCharges--;
        dashDirection = direction.normalized;
        Debug.Log($"Dash START (charges left: {currentDashCharges})");

        // Signal dash start to animator
        if (_animBridge != null) _animBridge.FlagDashStarted();

        // Face dash direction immediately
        if (faceMoveDirection && dashDirection.sqrMagnitude > 0.0001f)
        {
            Quaternion target = Quaternion.LookRotation(new Vector3(dashDirection.x, 0f, dashDirection.z), Vector3.up);
            transform.rotation = target;
        }
        
        // Play sound
        if (dashSounds.Length > 0)
        {
            AudioClip randomSound = dashSounds[Random.Range(0, dashSounds.Length)];
            audioSource.PlayOneShot(randomSound, dashVolume);
        }
        
        // Play dash particles
        if (dashParticles != null)
        {
            var emission = dashParticles.emission;
            emission.enabled = true;
            dashParticles.Clear(true);
            dashParticles.Play(true);
        }
    }

    void ProjectDownToGround()
    {
        Vector3 origin = transform.position + Vector3.up * groundRayHeight;
        if (Physics.Raycast(origin, Vector3.down, out var hit, groundRayHeight * 2f, groundMask))
        {
            const float skin = 0.01f;
            // Prefer collider bottom if present; else renderer bounds bottom
            Collider col = GetComponent<Collider>() ?? GetComponentInChildren<Collider>();
            float bottomWorldY = float.NaN;
            if (col != null)
            {
                bottomWorldY = col.bounds.min.y;
            }
            else
            {
                var rends = GetComponentsInChildren<Renderer>();
                if (rends != null && rends.Length > 0)
                {
                    float minY = float.PositiveInfinity;
                    for (int i = 0; i < rends.Length; i++)
                    {
                        var b = rends[i].bounds;
                        if (b.size.sqrMagnitude <= 0f) continue;
                        if (b.min.y < minY) minY = b.min.y;
                    }
                    bottomWorldY = minY;
                }
            }
            if (!float.IsNaN(bottomWorldY) && bottomWorldY < float.PositiveInfinity)
            {
                float delta = (hit.point.y + Mathf.Max(groundOffset, skin)) - bottomWorldY;
                transform.position += new Vector3(0f, delta, 0f);
            }
            else
            {
                // Fallback: set pivot to ground + offset
                float targetY = hit.point.y + groundOffset;
                transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
            }
        }
    }
    
    void UpdateDashCharges()
    {
        if (currentDashCharges < maxDashCharges)
        {
            rechargeTimer += Time.deltaTime;
            if (rechargeTimer >= dashRechargeTime)
            {
                currentDashCharges++;
                rechargeTimer = 0f;
            }
        }
    }
    #endregion
    
    #region HEALTH & COMBAT
    // Contact damage moved to Enemy; player no longer takes automatic contact damage here
    
    public void TakeDamage(GameObject source, int damage)
    {
        if (!IsAlive) return;
        
        // Check cooldown
        if (damageCooldowns.ContainsKey(source) && Time.time < damageCooldowns[source])
        {
            return;
        }
        
        // Apply damage
        currentHealth = Mathf.Max(0, currentHealth - damage);
        damageCooldowns[source] = Time.time + damageCooldown;
        onDamaged?.Invoke(damage);
        
        // Play effects
        if (damageSound != null)
        {
            audioSource.PlayOneShot(damageSound);
        }
        
        if (!isFlashing)
        {
            StartCoroutine(FlashEffect());
        }
        
        // Camera shake on damage
        var cameraController = Object.FindFirstObjectByType<CameraController>();
        if (cameraController != null)
        {
            // Simple nudge/shake effect can be added later if desired
            // For now, no-op to avoid dependency
        }
        
        // Notify
        onHealthChanged?.Invoke(currentHealth);
        
        // Check death
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // Reflect damage to attacker if applicable
            if (source != null)
            {
                var enemyHealth = source.GetComponentInParent<EnemyHealth>();
                if (enemyHealth != null)
                {
                    int reflected = 0;
                    if (reflectFlatCurrent > 0f) reflected += Mathf.RoundToInt(reflectFlatCurrent);
                    if (reflectPercentCurrent > 0f)
                    {
                        reflected += Mathf.RoundToInt(damage * (reflectPercentCurrent / 100f));
                    }
                    if (reflected > 0)
                    {
                        enemyHealth.TakeReflectDamage(reflected);
                    }
                }
            }
        }
    }
    
    public void Heal(int amount)
    {
        if (!IsAlive) return;
        
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        onHealthChanged?.Invoke(currentHealth);
    }
    
    void Die()
    {
        if (deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }
        
        onPlayerDeath?.Invoke();
        Debug.Log("Player died!");
    }
    
    System.Collections.IEnumerator FlashEffect()
    {
        if (playerRenderer == null) yield break;
        
        isFlashing = true;
        
        for (int i = 0; i < flashCount; i++)
        {
            playerRenderer.material.color = damageColor;
            yield return new WaitForSeconds(flashDuration / (2 * flashCount));
            
            playerRenderer.material.color = originalColor;
            yield return new WaitForSeconds(flashDuration / (2 * flashCount));
        }
        
        isFlashing = false;
    }
    
    void UpdateDamageCooldowns()
    {
        var expiredCooldowns = new List<GameObject>();
        foreach (var kvp in damageCooldowns)
        {
            if (Time.time >= kvp.Value)
            {
                expiredCooldowns.Add(kvp.Key);
            }
        }
        
        foreach (var enemy in expiredCooldowns)
        {
            damageCooldowns.Remove(enemy);
        }
    }
    #endregion
    
    #region CURRENCY
    public void AddCoins(int amount)
    {
        coins += amount;
        onCoinsChanged?.Invoke(coins);
    }
    
    public bool SpendCoins(int amount)
    {
        if (coins >= amount)
        {
            coins -= amount;
            onCoinsChanged?.Invoke(coins);
            return true;
        }
        return false;
    }
    
    // Upgrades removed; coins remain for other uses
    #endregion
    
    #region EQUIPMENT
    public bool TryEquip(EquipmentData item)
    {
        if (item == null) return false;
        if (characterEquipment == null) characterEquipment = GetComponent<CharacterEquipment>();
        return characterEquipment != null && characterEquipment.TryEquip(item);
    }
    
    public EquipmentData UnequipSlot(EquipmentType slotType)
    {
        if (characterEquipment == null) characterEquipment = GetComponent<CharacterEquipment>();
        if (characterEquipment == null) return null;
        return characterEquipment.UnequipSlot(slotType);
    }
    
    public List<EquipmentData> GetAllEquippedItems()
    {
        if (characterEquipment == null) characterEquipment = GetComponent<CharacterEquipment>();
        return characterEquipment != null ? characterEquipment.GetAllEquippedItems() : new List<EquipmentData>();
    }
    
    public List<StatModifier> GetAllStatModifiers()
    {
        if (characterEquipment == null) characterEquipment = GetComponent<CharacterEquipment>();
        return characterEquipment != null ? characterEquipment.GetAllStatModifiers() : new List<StatModifier>();
    }
    
    void HandleEquipmentChanged()
    {
        RecomputeAndApplyStats();
        if (enableStatLogs)
        {
            var names = GetAllEquippedItems();
            Debug.Log("[Stats] Equipped: " + string.Join(", ", names.ConvertAll(i => i != null ? i.equipmentName : "(null)")));
        }
        onEquipmentChanged?.Invoke();
    }
    #endregion

    #region STATS
    public void RecomputeAndApplyStats()
    {
        // Start from base values
        float flatMove = 0f;
        float pctMove = 0f; // as 0..100
        float flatDamage = 0f;
        float pctDamage = 0f;
        float pctAttackSpeed = 0f;
        float flatHealth = 0f;
        float pctHealth = 0f;
        float reflectFlat = 0f;
        float reflectPercent = 0f;
        float regenPerTick = 0f;   // amount per tick
        float regenTickSeconds = 1f; // default 1 second per tick
        float critChanceFlat = 0f;        // percentage points added
        float critChancePercent = 0f;     // % increased chance
        float critMultiFlat = 0f;         // percentage points added to multiplier
        float critMultiPercent = 0f;      // % increased multiplier

        if (characterEquipment == null) characterEquipment = GetComponent<CharacterEquipment>();
        if (characterEquipment != null)
        {
            var mods = GetAllStatModifiers();
            for (int i = 0; i < mods.Count; i++)
            {
                var m = mods[i];
                if (m.statType == StatType.MovementSpeedFlat)
                {
                    flatMove += m.value;
                }
                else if (m.statType == StatType.MovementSpeedPercent)
                {
                    pctMove += m.value;
                }
                else if (m.statType == StatType.Damage)
                {
                    if (m.isPercentage) pctDamage += m.value; else flatDamage += m.value;
                }
                else if (m.statType == StatType.DamageFlat)
                {
                    flatDamage += m.value;
                }
                else if (m.statType == StatType.DamagePercent)
                {
                    pctDamage += m.value;
                }
                else if (m.statType == StatType.AttackSpeed)
                {
                    // inherent percent
                    pctAttackSpeed += m.value;
                }
                else if (m.statType == StatType.Health)
                {
                    if (m.isPercentage) pctHealth += m.value; else flatHealth += m.value; // legacy support
                }
                else if (m.statType == StatType.MaxHealth)
                {
                    // treat as flat increased maximum health
                    flatHealth += m.value;
                }
                else if (m.statType == StatType.MaxHealthPercent)
                {
                    // treat as % increased maximum health
                    pctHealth += m.value;
                }
                else if (m.statType == StatType.DamageReflectFlat)
                {
                    reflectFlat += m.value;
                }
                else if (m.statType == StatType.DamageReflectPercent)
                {
                    reflectPercent += m.value;
                }
                else if (m.statType == StatType.HealthRegeneration)
                {
                    // Interpret as: value = amount per tick; use default tick time 1s for now
                    // If percentage regen is desired in future, split stat like others
                    regenPerTick += m.value;
                }
                else if (m.statType == StatType.CriticalChance)
                {
                    // legacy single type: treat as percent increased chance
                    critChancePercent += m.value;
                }
                else if (m.statType == StatType.CriticalMultiplier)
                {
                    // legacy single type: treat as percent increased multiplier
                    critMultiPercent += m.value;
                }
                
            }
        }

        // Compute movement speed
        float computedMove = (baseMoveSpeed + flatMove) * (1f + (pctMove / 100f));
        moveSpeed = Mathf.Max(0.1f, computedMove);

        // If a NavMeshAgent is used for click-to-move, mirror the speed
        var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            agent.speed = moveSpeed;
        }

        // Apply combat stats to AutoShooter(s)
        var shooters = GetComponentsInChildren<AutoShooter>();
        for (int i = 0; i < shooters.Length; i++)
        {
            var s = shooters[i];
            if (s != null)
            {
                // Compute final crit values from base defaults (2% chance, 150% multiplier)
                float baseCritChance = 2f;   // percentage points
                float baseCritMult = 150f;   // percent multiplier

                float finalCritChance = Mathf.Max(0f, baseCritChance + critChanceFlat);
                finalCritChance *= (1f + Mathf.Max(0f, critChancePercent) * 0.01f);

                float finalCritMult = Mathf.Max(0f, baseCritMult + critMultiFlat);
                finalCritMult *= (1f + Mathf.Max(0f, critMultiPercent) * 0.01f);

                s.ApplyStatModifiers(flatDamage, pctDamage, pctAttackSpeed, finalCritChance, finalCritMult);
            }
        }

        // Compute max health: percent applies only to increased pool (flatHealth), not base
        int bonusFromPercent = Mathf.RoundToInt(flatHealth * (pctHealth / 100f));
        int computedMaxHealth = Mathf.Max(1, baseMaxHealth + Mathf.RoundToInt(flatHealth) + bonusFromPercent);
        if (computedMaxHealth != maxHealth)
        {
            float ratio = maxHealth > 0 ? (float)currentHealth / maxHealth : 1f;
            maxHealth = computedMaxHealth;
            currentHealth = Mathf.Clamp(Mathf.RoundToInt(ratio * maxHealth), 1, maxHealth);
            onHealthChanged?.Invoke(currentHealth);
        }

        // Include skill-based reflect (from PlayerSkillHooks) before caching
        var skillHooks = GetComponent<PlayerSkillHooks>();
        if (skillHooks != null)
        {
            reflectFlat += skillHooks.GetSkillReflectFlat();
            reflectPercent += skillHooks.GetSkillReflectPercent();
        }

        LogStats(
            baseMoveSpeed, flatMove, pctMove,
            baseMaxHealth, Mathf.RoundToInt(flatHealth), bonusFromPercent, maxHealth, currentHealth,
            flatDamage, pctDamage, pctAttackSpeed,
            "Recompute (equipment changed)");

        // Cache reflect stats for use during damage events
        reflectFlatCurrent = reflectFlat;
        reflectPercentCurrent = reflectPercent;

        // Apply/refresh passive health regeneration
        ConfigureHealthRegen(regenPerTick, regenTickSeconds);
    }
    #endregion

    #region HEALTH REGEN
    private Coroutine regenRoutine;
    void ConfigureHealthRegen(float amountPerTick, float tickSeconds)
    {
        if (regenRoutine != null)
        {
            StopCoroutine(regenRoutine);
            regenRoutine = null;
        }
        regenPerTickCurrent = Mathf.RoundToInt(Mathf.Max(0f, amountPerTick));
        regenTickSecondsCurrent = Mathf.Max(0.01f, tickSeconds);
        if (regenPerTickCurrent > 0 && regenTickSecondsCurrent > 0.01f)
        {
            regenRoutine = StartCoroutine(DoHealthRegen(regenPerTickCurrent, regenTickSecondsCurrent));
        }
    }

    System.Collections.IEnumerator DoHealthRegen(int amountPerTick, float tickSeconds)
    {
        var wait = new WaitForSeconds(tickSeconds);
        while (true)
        {
            yield return wait;
            if (!IsAlive) continue;
            if (currentHealth >= maxHealth) continue;
            int before = currentHealth;
            currentHealth = Mathf.Min(maxHealth, currentHealth + Mathf.Max(1, amountPerTick));
            if (currentHealth != before) onHealthChanged?.Invoke(currentHealth);
        }
    }
    #endregion

    #region EXPOSED CURRENT STATS
    public int CurrentRegenPerTick => regenPerTickCurrent;
    public float CurrentRegenTickSeconds => regenTickSecondsCurrent;

    public struct PlayerComputedStats
    {
        public float finalMoveSpeed;
        public int finalMaxHealth;
        public int currentHealth;
        public float damageFlat;
        public float damagePercent;
        public float attackSpeedPercent;
        public float critChancePercent;     // final
        public float critMultiplierPercent; // final
        public float reflectFlat;
        public float reflectPercent;
        public int regenPerTick;
        public float regenTickSeconds;
    }

    public PlayerComputedStats GetComputedStats()
    {
        float flatMove = 0f;
        float pctMove = 0f;
        float flatDamage = 0f;
        float pctDamage = 0f;
        float pctAttackSpeed = 0f;
        float flatHealth = 0f;
        float pctHealth = 0f;
        float reflectFlat = 0f;
        float reflectPercent = 0f;
        float critChanceFlat = 0f;
        float critChancePercent = 0f;
        float critMultiFlat = 0f;
        float critMultiPercent = 0f;
        float regenPerTick = 0f;
        float regenTickSeconds = 1f;

        var mods = GetAllStatModifiers();
        for (int i = 0; i < mods.Count; i++)
        {
            var m = mods[i];
            if (m.statType == StatType.MovementSpeedFlat) flatMove += m.value;
            else if (m.statType == StatType.MovementSpeedPercent) pctMove += m.value;
            else if (m.statType == StatType.Damage) { if (m.isPercentage) pctDamage += m.value; else flatDamage += m.value; }
            else if (m.statType == StatType.DamageFlat) flatDamage += m.value;
            else if (m.statType == StatType.DamagePercent) pctDamage += m.value;
            else if (m.statType == StatType.AttackSpeed) pctAttackSpeed += m.value;
            else if (m.statType == StatType.Health) { if (m.isPercentage) pctHealth += m.value; else flatHealth += m.value; }
            else if (m.statType == StatType.MaxHealth) flatHealth += m.value;
            else if (m.statType == StatType.MaxHealthPercent) pctHealth += m.value;
            else if (m.statType == StatType.DamageReflectFlat) reflectFlat += m.value;
            else if (m.statType == StatType.DamageReflectPercent) reflectPercent += m.value;
            else if (m.statType == StatType.CriticalChance) critChancePercent += m.value;
            else if (m.statType == StatType.CriticalMultiplier) critMultiPercent += m.value;
            else if (m.statType == StatType.HealthRegeneration) regenPerTick += m.value;
        }

        float computedMove = (baseMoveSpeed + flatMove) * (1f + (pctMove / 100f));

        float baseCritChance = 2f;
        float baseCritMult = 150f;
        float finalCritChance = Mathf.Max(0f, baseCritChance + critChanceFlat);
        finalCritChance *= (1f + Mathf.Max(0f, critChancePercent) * 0.01f);
        float finalCritMult = Mathf.Max(0f, baseCritMult + critMultiFlat);
        finalCritMult *= (1f + Mathf.Max(0f, critMultiPercent) * 0.01f);

        int bonusFromPercent = Mathf.RoundToInt(flatHealth * (pctHealth / 100f));
        int finalMaxHp = Mathf.Max(1, baseMaxHealth + Mathf.RoundToInt(flatHealth) + bonusFromPercent);

        return new PlayerComputedStats
        {
            finalMoveSpeed = Mathf.Max(0.1f, computedMove),
            finalMaxHealth = finalMaxHp,
            currentHealth = currentHealth,
            damageFlat = flatDamage,
            damagePercent = pctDamage,
            attackSpeedPercent = pctAttackSpeed,
            critChancePercent = finalCritChance,
            critMultiplierPercent = finalCritMult,
            reflectFlat = reflectFlat,
            reflectPercent = reflectPercent,
            regenPerTick = Mathf.RoundToInt(regenPerTick),
            regenTickSeconds = regenTickSeconds
        };
    }
    #endregion
    
    #region DEBUG
    [ContextMenu("Print Player Status")]
    public void PrintPlayerStatus()
    {
        Debug.Log($"=== PLAYER STATUS ===");
        Debug.Log($"Health: {currentHealth}/{maxHealth}");
        Debug.Log($"Coins: {coins}");
        Debug.Log($"Dash Charges: {currentDashCharges}/{maxDashCharges}");
        Debug.Log($"Equipped Items: {GetAllEquippedItems().Count}");
    }
    #endregion
}

