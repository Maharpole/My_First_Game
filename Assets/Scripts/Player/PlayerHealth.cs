using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("Maximum health of the player")]
    public int maxHealth = 100;
    
    [Tooltip("Current health of the player")]
    public int currentHealth;
    
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
    public UnityEvent onDamage;
    public UnityEvent onDeath;
    
    // Dictionary to track damage cooldowns for each enemy
    private Dictionary<GameObject, float> damageCooldowns = new Dictionary<GameObject, float>();
    private Rigidbody rb;
    private Renderer[] playerRenderers;
    private Color[] originalColors;
    private Material[] originalMaterials;
    private AudioSource audioSource;
    private bool isFlashing = false;
    private GameManager gameManager;
    
    void Start()
    {
        // Initialize health
        currentHealth = maxHealth;
        
        // Find the GameManager
        gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("GameManager not found in the scene!");
        }
        
        // Set up Rigidbody
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.freezeRotation = true;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
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
    
    void TakeDamage(GameObject enemy)
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
        currentHealth -= contactDamage;
        
        // Set cooldown for this enemy
        damageCooldowns[enemy] = Time.time + damageCooldown;
        
        // Play damage sound
        if (damageSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(damageSound);
        }
        
        // Trigger damage event
        onDamage.Invoke();
        
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
    
    void Die()
    {
        // Play death sound
        if (deathSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(deathSound);
        }
        
        // Trigger death event
        onDeath.Invoke();
        
        // Notify the GameManager
        if (gameManager != null)
        {
            gameManager.GameOver();
        }
        else
        {
            Debug.LogError("GameManager not found when trying to end the game!");
        }
        
        // Disable the player
        gameObject.SetActive(false);
    }
    
    // Optional: Add a method to heal the player
    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }
    
    void OnDestroy()
    {
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
} 