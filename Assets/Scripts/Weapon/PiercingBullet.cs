using UnityEngine;
using System.Collections.Generic;

public class PiercingBullet : Bullet
{
    [Header("Piercing Settings")]
    public int maxPierceCount = 3;
    public float pierceDamageMultiplier = 0.8f;
    
    private int currentPierceCount = 0;
    private List<GameObject> hitEnemies = new List<GameObject>();
    
    public override void OnTriggerEnter(Collider other)
    {
        // Skip if the collider is the player
        if (other.CompareTag("Player")) return;
        
        // Check if we hit an enemy
        if (other.CompareTag("Enemy"))
        {
            // Check if we've already hit this enemy
            if (hitEnemies.Contains(other.gameObject))
            {
                return;
            }
            
            // Add to hit list
            hitEnemies.Add(other.gameObject);
            
            // Get the enemy's health component
            EnemyHealth enemyHealth = other.GetComponent<EnemyHealth>();
            
            if (enemyHealth != null)
            {
                // Calculate damage with pierce reduction
                float pierceDamage = damage * Mathf.Pow(pierceDamageMultiplier, currentPierceCount);
                
                // Deal damage to the enemy
                enemyHealth.TakeDamage(Mathf.RoundToInt(pierceDamage));
            }
            
            // Increment pierce count
            currentPierceCount++;
            
            // Check if we've reached max pierce count
            if (currentPierceCount >= maxPierceCount)
            {
                // Destroy the bullet
                Destroy(gameObject);
            }
        }
        else
        {
            // If we hit something that's not an enemy, destroy the bullet
            Destroy(gameObject);
        }
    }
} 