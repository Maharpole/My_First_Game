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
    
    [Header("== HEALTH & COMBAT ==")]
    public int maxHealth = 100;
    [SerializeField] private int currentHealth = 100;
    public float damageCooldown = 1f;
    public int contactDamage = 10;
    
    [Header("== CURRENCY ==")]
    [SerializeField] private int coins = 0;
    
    [Header("== EQUIPMENT ==")]
    public EquipmentSlot helmet = new EquipmentSlot(EquipmentType.Helmet, "Helmet");
    public EquipmentSlot amulet = new EquipmentSlot(EquipmentType.Amulet, "Amulet");
    public EquipmentSlot gloves = new EquipmentSlot(EquipmentType.Gloves, "Gloves");
    public EquipmentSlot ringLeft = new EquipmentSlot(EquipmentType.RingLeft, "Ring (L)");
    public EquipmentSlot ringRight = new EquipmentSlot(EquipmentType.RingRight, "Ring (R)");
    public EquipmentSlot boots = new EquipmentSlot(EquipmentType.Boots, "Boots");
    public EquipmentSlot belt = new EquipmentSlot(EquipmentType.Belt, "Belt");
    
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
    private int currentDashCharges;
    private float rechargeTimer = 0f;
    private Dictionary<GameObject, float> damageCooldowns = new Dictionary<GameObject, float>();
    private Renderer playerRenderer;
    private Color originalColor;
    private AudioSource audioSource;
    private bool isFlashing = false;
    private Dictionary<EquipmentType, EquipmentSlot> equipmentSlots;
    
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
        
        // Initialize health
        currentHealth = maxHealth;
        
        // Initialize equipment slots
        equipmentSlots = new Dictionary<EquipmentType, EquipmentSlot>
        {
            { EquipmentType.Helmet, helmet },
            { EquipmentType.Amulet, amulet },
            { EquipmentType.Gloves, gloves },
            { EquipmentType.RingLeft, ringLeft },
            { EquipmentType.RingRight, ringRight },
            { EquipmentType.Boots, boots },
            { EquipmentType.Belt, belt }
        };
        
        // Subscribe to equipment events
        foreach (var slot in equipmentSlots.Values)
        {
            slot.onItemEquipped.AddListener(OnEquipmentChanged);
            slot.onItemUnequipped.AddListener(OnEquipmentChanged);
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
        
        Debug.Log("Player initialized successfully!");
    }
    
    void Update()
    {
        HandleMovement();
        UpdateDashCharges();
        UpdateDamageCooldowns();
    }
    
    #region MOVEMENT
    void HandleMovement()
    {
        if (isDashing)
        {
            dashTime += Time.deltaTime;
            if (dashTime >= dashDuration)
            {
                isDashing = false;
                if (dashParticles != null)
                {
                    var emission = dashParticles.emission;
                    emission.enabled = false;
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
        
        // Move player
        transform.Translate(direction * dashSpeed * Time.deltaTime);
        
        // Play sound
        if (dashSounds.Length > 0)
        {
            AudioClip randomSound = dashSounds[Random.Range(0, dashSounds.Length)];
            audioSource.PlayOneShot(randomSound, dashVolume);
        }
        
        // Play particles
        if (dashParticles != null)
        {
            var emission = dashParticles.emission;
            emission.enabled = true;
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
        var cameraController = FindObjectOfType<CameraController>();
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
        
        // Special handling for rings
        if (item.equipmentType == EquipmentType.RingLeft || item.equipmentType == EquipmentType.RingRight)
        {
            if (ringLeft.IsEmpty) return ringLeft.TryEquip(item);
            if (ringRight.IsEmpty) return ringRight.TryEquip(item);
            return ringLeft.TryEquip(item); // Replace left ring
        }
        
        // Normal equipment
        if (equipmentSlots.TryGetValue(item.equipmentType, out EquipmentSlot slot))
        {
            return slot.TryEquip(item);
        }
        
        return false;
    }
    
    public EquipmentData UnequipSlot(EquipmentType slotType)
    {
        if (equipmentSlots.TryGetValue(slotType, out EquipmentSlot slot))
        {
            return slot.Unequip();
        }
        return null;
    }
    
    public List<EquipmentData> GetAllEquippedItems()
    {
        var items = new List<EquipmentData>();
        foreach (var slot in equipmentSlots.Values)
        {
            if (slot.HasItem)
            {
                items.Add(slot.EquippedItem);
            }
        }
        return items;
    }
    
    public List<StatModifier> GetAllStatModifiers()
    {
        var allModifiers = new List<StatModifier>();
        foreach (var item in GetAllEquippedItems())
        {
            allModifiers.AddRange(item.AllStats);
        }
        return allModifiers;
    }
    
    void OnEquipmentChanged(EquipmentData item)
    {
        onEquipmentChanged?.Invoke();
        Debug.Log($"Equipment changed: {item.equipmentName}");
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
        Debug.Log($"Equipment: {GetAllEquippedItems().Count}/7 slots filled");
        
        foreach (var slot in equipmentSlots.Values)
        {
            string status = slot.HasItem ? slot.EquippedItem.equipmentName : "Empty";
            Debug.Log($"  {slot.slotName}: {status}");
        }
    }
    #endregion
}

