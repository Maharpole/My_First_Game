using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance { get; private set; }

    [Header("Player Stats")]
    public int currentCoins = 0;
    public int maxHealth = 100;
    public int currentHealth = 100;

    [Header("Damage Settings")]
    [Tooltip("How long before you can take damage from the same enemy again (in seconds)")]
    public float damageCooldown = 1f;
    
    [Tooltip("How much damage enemies do on contact")]
    public int contactDamage = 10;

    [Header("Visual Effects")]
    [Tooltip("Color to flash when taking damage")]
    public Color damageColor = Color.red;
    
    [Tooltip("Duration of the damage flash effect")]
    public float flashDuration = 0.2f;
    
    [Tooltip("Number of times to flash")]
    public int flashCount = 3;

    [Header("Sound Effects")]
    [Tooltip("Sound to play when taking damage")]
    public AudioClip damageSound;
    
    [Tooltip("Sound to play when dying")]
    public AudioClip deathSound;

    [Header("Events")]
    public UnityEvent<int> onCoinsChanged;
    public UnityEvent<int> onHealthChanged;
    public UnityEvent onPlayerDeath;
    public UnityEvent onDamage;

    private Dictionary<WeaponData, List<WeaponData.WeaponUpgrade>> purchasedUpgrades = new Dictionary<WeaponData, List<WeaponData.WeaponUpgrade>>();
    private List<WeaponData> activeWeapons = new List<WeaponData>();
    private Dictionary<GameObject, float> damageCooldowns = new Dictionary<GameObject, float>();
    private Renderer[] playerRenderers;
    private Color[] originalColors;
    private Material[] originalMaterials;
    private AudioSource audioSource;
    private bool isFlashing = false;
    private GameManager gameManager;

    private void Awake()
    {
        // Singleton pattern with DontDestroyOnLoad
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
            InitializeHealthSystem();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeHealthSystem()
    {
        // Find the GameManager
        gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("GameManager not found in the scene!");
        }

        // Get all renderer components (including children)
        playerRenderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[playerRenderers.Length];
        originalMaterials = new Material[playerRenderers.Length];
        
        for (int i = 0; i < playerRenderers.Length; i++)
        {
            if (playerRenderers[i] != null)
            {
                originalMaterials[i] = playerRenderers[i].material;
                originalColors[i] = playerRenderers[i].material.color;
            }
        }
        
        // Set up audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        // Clean up the materials if we created new ones
        if (originalMaterials != null)
        {
            foreach (var material in originalMaterials)
            {
                if (material != null)
                {
                    Destroy(material);
                }
            }
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Initialize any scene-specific data here
        InitializeHealthSystem();
    }

    void Update()
    {
        // Update cooldowns
        List<GameObject> expiredCooldowns = new List<GameObject>();
        foreach (var kvp in damageCooldowns)
        {
            if (Time.time >= kvp.Value)
            {
                expiredCooldowns.Add(kvp.Key);
            }
        }
        
        // Remove expired cooldowns
        foreach (var enemy in expiredCooldowns)
        {
            damageCooldowns.Remove(enemy);
        }
    }

    void OnCollisionStay(Collision collision)
    {
        // Check if colliding with an enemy
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Debug.Log("Collision detected with: " + collision.gameObject.name);
            TakeDamage(collision.gameObject);
        }
    }

    // Coin Management
    public void AddCoins(int amount)
    {
        currentCoins += amount;
        onCoinsChanged?.Invoke(currentCoins);
    }

    public bool SpendCoins(int amount)
    {
        if (currentCoins >= amount)
        {
            currentCoins -= amount;
            onCoinsChanged?.Invoke(currentCoins);
            return true;
        }
        return false;
    }

    // Health Management
    public void TakeDamage(GameObject enemy)
    {
        // Check if we're on cooldown for this enemy
        if (damageCooldowns.ContainsKey(enemy))
        {
            if (Time.time < damageCooldowns[enemy])
            {
                return; // Still on cooldown
            }
        }
        
        // Apply damage
        currentHealth = Mathf.Max(0, currentHealth - contactDamage);
        onHealthChanged?.Invoke(currentHealth);
        
        // Set cooldown for this enemy
        damageCooldowns[enemy] = Time.time + damageCooldown;
        
        // Play damage sound
        if (damageSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(damageSound);
        }
        
        // Trigger damage event
        onDamage?.Invoke();
        
        // Start flash effect if not already flashing
        if (!isFlashing)
        {
            StartCoroutine(FlashEffect());
        }
        
        // Check if player is dead
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    IEnumerator FlashEffect()
    {
        isFlashing = true;
        
        try
        {
            for (int i = 0; i < flashCount; i++)
            {
                // Flash all renderers to damage color
                for (int j = 0; j < playerRenderers.Length; j++)
                {
                    if (playerRenderers[j] != null)
                    {
                        playerRenderers[j].material.color = damageColor;
                    }
                }
                yield return new WaitForSeconds(flashDuration / (2 * flashCount));
                
                // Return all renderers to original color
                for (int j = 0; j < playerRenderers.Length; j++)
                {
                    if (playerRenderers[j] != null)
                    {
                        playerRenderers[j].material.color = originalColors[j];
                    }
                }
                yield return new WaitForSeconds(flashDuration / (2 * flashCount));
            }
        }
        finally
        {
            // Ensure we reset the colors even if there's an error
            for (int j = 0; j < playerRenderers.Length; j++)
            {
                if (playerRenderers[j] != null)
                {
                    playerRenderers[j].material.color = originalColors[j];
                }
            }
            isFlashing = false;
        }
    }

    private void Die()
    {
        // Play death sound
        if (deathSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(deathSound);
        }
        
        // Trigger death event
        onPlayerDeath?.Invoke();
        
        // Notify the GameManager
        if (gameManager != null)
        {
            gameManager.GameOver();
        }
        else
        {
            Debug.LogError("GameManager not found when trying to end the game!");
        }
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        onHealthChanged?.Invoke(currentHealth);
    }

    // Weapon Management
    public void AddWeapon(WeaponData weapon)
    {
        if (!activeWeapons.Contains(weapon))
        {
            activeWeapons.Add(weapon);
            purchasedUpgrades[weapon] = new List<WeaponData.WeaponUpgrade>();
        }
    }

    public List<WeaponData> GetActiveWeapons()
    {
        return new List<WeaponData>(activeWeapons);
    }

    public List<WeaponData.WeaponUpgrade> GetPurchasedUpgrades(WeaponData weapon)
    {
        if (purchasedUpgrades.TryGetValue(weapon, out var upgrades))
        {
            return new List<WeaponData.WeaponUpgrade>(upgrades);
        }
        return new List<WeaponData.WeaponUpgrade>();
    }

    public bool PurchaseUpgrade(WeaponData weapon, WeaponData.WeaponUpgrade upgrade)
    {
        if (!SpendCoins(upgrade.cost)) return false;

        if (!purchasedUpgrades.ContainsKey(weapon))
        {
            purchasedUpgrades[weapon] = new List<WeaponData.WeaponUpgrade>();
        }

        purchasedUpgrades[weapon].Add(upgrade);
        return true;
    }

    public bool CanAffordUpgrade(WeaponData.WeaponUpgrade upgrade)
    {
        return currentCoins >= upgrade.cost;
    }
} 