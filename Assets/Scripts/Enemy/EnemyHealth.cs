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
    
    [Tooltip("Particle system to play on death")]
    public ParticleSystem deathParticles;
    
    private Material originalMaterial;
    private Renderer enemyRenderer;
    private Color originalColor;
    
    [Header("Events")]
    public UnityEvent onDeath;
    public UnityEvent onDamage;

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
        
        // Flash red
        StartCoroutine(FlashRed());
        
        // Check if enemy is dead
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    IEnumerator FlashRed()
    {
        if (enemyRenderer != null)
        {
            // Change to red
            enemyRenderer.material.color = damageColor;
            
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