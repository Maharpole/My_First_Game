using UnityEngine;

public class SplittingBullet : Bullet
{
    [Header("Splitting Settings")]
    public int splitCount = 3;
    public float splitAngle = 30f;
    public float splitDamageMultiplier = 0.5f;
    public GameObject splitBulletPrefab;
    public GameObject splitEffect;
    public AudioClip splitSound;
    public float splitVolume = 1f;
    public float bulletSpeed = 20f;
    
    protected override void Start()
    {
        base.Start();
    }
    
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
            }
            
            // Split the bullet
            Split();
            
            // Destroy the original bullet
            Destroy(gameObject);
        }
    }
    
    void Split()
    {
        // Play split effect
        if (splitEffect != null)
        {
            Instantiate(splitEffect, transform.position, Quaternion.identity);
        }
        
        // Play split sound
        if (splitSound != null)
        {
            AudioSource.PlayClipAtPoint(splitSound, transform.position, splitVolume);
        }
        
        // Calculate angle step
        float angleStep = splitAngle / (splitCount > 1 ? splitCount - 1 : 1);
        float currentAngle = -splitAngle / 2f;
        
        // Get current direction
        Vector3 currentDirection = transform.forward;
        
        // Create split bullets
        for (int i = 0; i < splitCount; i++)
        {
            // Calculate split direction
            Vector3 splitDirection = Quaternion.Euler(0, currentAngle, 0) * currentDirection;
            currentAngle += angleStep;
            
            // Create split bullet
            GameObject splitBullet = Instantiate(splitBulletPrefab, transform.position, Quaternion.identity);
            
            // Set bullet's rotation to face the direction it's moving
            splitBullet.transform.rotation = Quaternion.LookRotation(splitDirection);
            
            // Get or add Rigidbody to bullet
            Rigidbody bulletRb = splitBullet.GetComponent<Rigidbody>();
            if (bulletRb == null)
            {
                bulletRb = splitBullet.AddComponent<Rigidbody>();
            }
            
            // Set bullet's velocity
            bulletRb.velocity = splitDirection * bulletSpeed;
            
            // Set bullet damage
            Bullet bulletComponent = splitBullet.GetComponent<Bullet>();
            if (bulletComponent != null)
            {
                bulletComponent.damage = damage * splitDamageMultiplier;
            }
        }
    }
} 