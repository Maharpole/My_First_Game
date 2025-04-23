using UnityEngine;
using System.Collections.Generic;

public class ChainLightningBullet : Bullet
{
    [Header("Chain Lightning Settings")]
    public int maxChainCount = 3;
    public float chainRange = 5f;
    public float chainDamageMultiplier = 0.7f;
    public GameObject lightningEffect;
    public AudioClip lightningSound;
    public float lightningVolume = 1f;
    public LayerMask enemyLayer;
    
    private int currentChainCount = 0;
    private List<GameObject> hitEnemies = new List<GameObject>();
    
    public override void OnTriggerEnter(Collider other)
    {
        // Skip if the collider is the player
        if (other.CompareTag("Player")) return;
        
        // Check if we hit an enemy
        if (other.CompareTag("Enemy"))
        {
            // Get the enemy's health component
            EnemyHealth enemyHealth = other.GetComponent<EnemyHealth>();
            
            if (enemyHealth != null)
            {
                // Deal damage to the enemy
                enemyHealth.TakeDamage(Mathf.RoundToInt(damage));
                
                // Add enemy to hit list
                hitEnemies.Add(other.gameObject);
                
                // Chain to nearby enemies
                ChainLightning(other.gameObject);
            }
            
            // Destroy the bullet
            Destroy(gameObject);
        }
    }
    
    void ChainLightning(GameObject sourceEnemy)
    {
        // Check if we've reached the maximum chain count
        if (currentChainCount >= maxChainCount)
            return;
            
        // Find nearby enemies
        Collider[] nearbyColliders = Physics.OverlapSphere(sourceEnemy.transform.position, chainRange, enemyLayer);
        
        GameObject closestEnemy = null;
        float closestDistance = float.MaxValue;
        
        // Find the closest enemy that hasn't been hit yet
        foreach (Collider collider in nearbyColliders)
        {
            if (collider.CompareTag("Enemy") && !hitEnemies.Contains(collider.gameObject))
            {
                float distance = Vector3.Distance(sourceEnemy.transform.position, collider.transform.position);
                
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemy = collider.gameObject;
                }
            }
        }
        
        // If we found a target, chain to it
        if (closestEnemy != null)
        {
            // Increment chain count
            currentChainCount++;
            
            // Create lightning effect
            if (lightningEffect != null)
            {
                GameObject effect = Instantiate(lightningEffect, sourceEnemy.transform.position, Quaternion.identity);
                
                // Position the effect between the source and target
                effect.transform.position = (sourceEnemy.transform.position + closestEnemy.transform.position) / 2f;
                
                // Rotate the effect to face the target
                effect.transform.LookAt(closestEnemy.transform);
                
                // Scale the effect to match the distance
                float distance = Vector3.Distance(sourceEnemy.transform.position, closestEnemy.transform.position);
                effect.transform.localScale = new Vector3(effect.transform.localScale.x, effect.transform.localScale.y, distance);
            }
            
            // Play lightning sound
            if (lightningSound != null)
            {
                AudioSource.PlayClipAtPoint(lightningSound, sourceEnemy.transform.position, lightningVolume);
            }
            
            // Deal damage to the chained enemy
            EnemyHealth enemyHealth = closestEnemy.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                // Calculate chain damage
                float chainDamage = damage * Mathf.Pow(chainDamageMultiplier, currentChainCount);
                enemyHealth.TakeDamage(Mathf.RoundToInt(chainDamage));
                
                // Add enemy to hit list
                hitEnemies.Add(closestEnemy);
                
                // Continue the chain
                ChainLightning(closestEnemy);
            }
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw the chain range in the editor
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chainRange);
    }
} 