using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

/// <summary>
/// Single, unified player script that handles everything player-related.
/// No more multiple scripts - this is the ONLY script you need on your player.
/// </summary>
public class Player : MonoBehaviour
{
    public static Player Instance { get; private set; }
    
    [Header("== MOVEMENT ==")]
    public float moveSpeed = 5f;
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
    public int contactDamage = 10;
    
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
        private float baseMoveSpeed;
        private int baseMaxHealth;
        [Header("== STAT SCALARS ==")]
        [Tooltip("How much max health is granted per 1 point of Vitality")] public int healthPerVitality = 1;
    
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
        baseMoveSpeed = moveSpeed; // remember unmodified baseline for stat calculations
        
        // Initialize health
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
        
        // Initialize particle emission
        if (dashParticles != null)
        {
            var emission = dashParticles.emission;
            emission.enabled = false;
        }
        
        // Apply initial stats from any pre-equipped gear
        RecomputeAndApplyStats();
        Debug.Log("Player initialized successfully!");
    }
    
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
                // Stop dash particles after dash ends
                if (dashParticles != null)
                {
                    var emission = dashParticles.emission;
                    emission.enabled = false;
                    dashParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }
            return;
        }
        
        // Get input
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 movement = new Vector3(horizontal, 0, vertical).normalized;
        
        // Move the player
        if (movement.magnitude > 0.1f)
        {
            transform.Translate(movement * moveSpeed * Time.deltaTime);
        }
        
        // Handle dash
        if (Input.GetKeyDown(KeyCode.Space) && currentDashCharges > 0 && movement.magnitude > 0.1f)
        {
            PerformDash(movement);
        }
    }
    
    void PerformDash(Vector3 direction)
    {
        isDashing = true;
        dashTime = 0f;
        currentDashCharges--;
        dashDirection = direction.normalized;
        
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
            Vector3 target = hit.point + Vector3.up * groundOffset;
            transform.position = new Vector3(transform.position.x, target.y, transform.position.z);
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
    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            TakeDamage(collision.gameObject, contactDamage);
        }
    }
    
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
    
    public bool CanAffordUpgrade(WeaponData.WeaponUpgrade upgrade)
    {
        return coins >= upgrade.cost;
    }
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
        float flatVitality = 0f;
        float flatHealth = 0f;
        float pctHealth = 0f;

        if (characterEquipment == null) characterEquipment = GetComponent<CharacterEquipment>();
        if (characterEquipment != null)
        {
            var mods = GetAllStatModifiers();
            for (int i = 0; i < mods.Count; i++)
            {
                var m = mods[i];
                if (m.statType == StatType.MovementSpeed)
                {
                    if (m.isPercentage) pctMove += m.value; else flatMove += m.value;
                }
                else if (m.statType == StatType.Damage)
                {
                    if (m.isPercentage) pctDamage += m.value; else flatDamage += m.value;
                }
                else if (m.statType == StatType.AttackSpeed)
                {
                    // treat as percentage
                    pctAttackSpeed += m.value;
                }
                else if (m.statType == StatType.Vitality)
                {
                    // treat vitality as flat points
                    flatVitality += m.value;
                }
                else if (m.statType == StatType.Health)
                {
                    if (m.isPercentage) pctHealth += m.value; else flatHealth += m.value;
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
                s.ApplyStatModifiers(flatDamage, pctDamage, pctAttackSpeed);
            }
        }

        // Compute max health from Vitality and Health modifiers
        int computedMaxHealth = Mathf.Max(1, Mathf.RoundToInt(((baseMaxHealth + flatHealth) + (flatVitality * healthPerVitality)) * (1f + (pctHealth / 100f))));
        if (computedMaxHealth != maxHealth)
        {
            float ratio = maxHealth > 0 ? (float)currentHealth / maxHealth : 1f;
            maxHealth = computedMaxHealth;
            currentHealth = Mathf.Clamp(Mathf.RoundToInt(ratio * maxHealth), 1, maxHealth);
            onHealthChanged?.Invoke(currentHealth);
        }
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

