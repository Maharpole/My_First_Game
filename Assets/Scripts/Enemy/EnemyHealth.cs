using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("Maximum health of the enemy")]
    public int maxHealth = 10;
    
    [Tooltip("Current health of the enemy")]
    public int currentHealth;
    
    [Header("Visual Effects")]
    [Tooltip("Duration of the damage flash effect")]
    public float flashDuration = 0.1f;
    
    [Tooltip("Color to flash when taking damage")]
    public Color damageColor = Color.red;
    
    [Tooltip("Color to flash when healing")]
    public Color healColor = Color.green;
    
    [Tooltip("Particle system to play on death")]
    public ParticleSystem deathParticles;
    
    private Material originalMaterial;
    private Renderer enemyRenderer;
    private Color originalColor;
    
    [Header("Events")]
    public UnityEvent onDeath;
    public UnityEvent onDamage;
    public UnityEvent onHeal;

    void Start()
    {
        // Initialize health
        currentHealth = maxHealth;
        
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
            var controller = GetComponent<EnemyController>();
            var player = Object.FindFirstObjectByType<Player>();
            if (controller != null && player != null)
            {
                controller.ForceAggro(player.transform);
            }
        }
        
        // Flash red
        StartCoroutine(FlashColor(damageColor));
        
        // Debug logging for tuning
        CombatDebug.LogDamage(gameObject, damage, Mathf.Max(0, currentHealth), maxHealth);

        // Check if enemy is dead
        if (currentHealth <= 0)
        {
            Die();
        }
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
        // Trigger death event
        onDeath.Invoke();
        // Also notify aggro group to avoid lingering idle
        var group = GetComponentInParent<AggroGroup>();
        if (group != null)
        {
            group.OnMemberDamaged();
        }
        
        // Play death particles if available
        if (deathParticles != null)
        {
            // Detach particles from enemy
            deathParticles.transform.parent = null;
            
            // Play the particles
            deathParticles.Play();
            
            // Destroy particles after they finish
            Destroy(deathParticles.gameObject, deathParticles.main.duration);
        }
        
        // Destroy the enemy
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
} 