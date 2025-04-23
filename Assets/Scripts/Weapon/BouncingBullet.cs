using UnityEngine;
using System.Collections.Generic;

public class BouncingBullet : Bullet
{
    [Header("Bouncing Settings")]
    public int maxBounces = 3;
    public float bounceSpeedMultiplier = 0.8f;
    public float bounceDamageMultiplier = 0.7f;
    public GameObject bounceEffect;
    public AudioClip bounceSound;
    public float bounceVolume = 1f;
    
    private int currentBounces = 0;
    private List<GameObject> hitEnemies = new List<GameObject>();
    private Rigidbody rb;
    
    protected override void Start()
    {
        base.Start();
        rb = GetComponent<Rigidbody>();
    }
    
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
                // Calculate damage with bounce reduction
                float bounceDamage = damage * Mathf.Pow(bounceDamageMultiplier, currentBounces);
                
                // Deal damage to the enemy
                enemyHealth.TakeDamage(Mathf.RoundToInt(bounceDamage));
            }
            
            // Find a new target to bounce to
            GameObject newTarget = FindNewTarget(other.gameObject);
            
            if (newTarget != null)
            {
                // Calculate direction to new target
                Vector3 direction = (newTarget.transform.position - transform.position).normalized;
                
                // Apply new velocity
                float currentSpeed = rb.velocity.magnitude * bounceSpeedMultiplier;
                rb.velocity = direction * currentSpeed;
                
                // Rotate to face new direction
                transform.rotation = Quaternion.LookRotation(direction);
                
                // Play bounce effect
                if (bounceEffect != null)
                {
                    Instantiate(bounceEffect, transform.position, Quaternion.identity);
                }
                
                // Play bounce sound
                if (bounceSound != null)
                {
                    AudioSource.PlayClipAtPoint(bounceSound, transform.position, bounceVolume);
                }
                
                // Increment bounce count
                currentBounces++;
                
                // Check if we've reached max bounces
                if (currentBounces >= maxBounces)
                {
                    // Destroy the bullet
                    Destroy(gameObject);
                }
            }
            else
            {
                // If no new target, destroy the bullet
                Destroy(gameObject);
            }
        }
        else
        {
            // If we hit something that's not an enemy, destroy the bullet
            Destroy(gameObject);
        }
    }
    
    GameObject FindNewTarget(GameObject currentTarget)
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject nearestEnemy = null;
        float nearestDistance = Mathf.Infinity;
        
        foreach (GameObject enemy in enemies)
        {
            // Skip if it's the current target or already hit
            if (enemy == currentTarget || hitEnemies.Contains(enemy))
                continue;
                
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestEnemy = enemy;
            }
        }
        
        return nearestEnemy;
    }
} 