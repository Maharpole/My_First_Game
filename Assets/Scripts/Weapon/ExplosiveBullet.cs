using UnityEngine;

public class ExplosiveBullet : Bullet
{
    [Header("Explosion Settings")]
    public float explosionRadius = 5f;
    public float explosionForce = 500f;
    public float explosionDamageMultiplier = 0.5f;
    public GameObject explosionEffect;
    public AudioClip explosionSound;
    public float explosionVolume = 1f;
    
    public override void OnTriggerEnter(Collider other)
    {
        // Skip if the collider is the player
        if (other.CompareTag("Player")) return;
        
        // Create explosion
        Explode();
        
        // Destroy the bullet
        Destroy(gameObject);
    }
    
    void Explode()
    {
        // Play explosion effect
        if (explosionEffect != null)
        {
            Instantiate(explosionEffect, transform.position, Quaternion.identity);
        }
        
        // Play explosion sound
        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, transform.position, explosionVolume);
        }
        
        // Find all colliders in explosion radius
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        
        foreach (Collider hit in colliders)
        {
            // Skip if it's the player
            if (hit.CompareTag("Player")) continue;
            
            // Apply explosion force to rigidbodies
            Rigidbody rb = hit.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
            }
            
            // Deal damage to enemies
            if (hit.CompareTag("Enemy"))
            {
                // Calculate distance-based damage
                float distance = Vector3.Distance(transform.position, hit.transform.position);
                float damageMultiplier = 1f - (distance / explosionRadius);
                damageMultiplier = Mathf.Clamp01(damageMultiplier);
                
                float explosionDamage = damage * explosionDamageMultiplier * damageMultiplier;
                
                // Apply damage
                EnemyHealth enemyHealth = hit.GetComponent<EnemyHealth>();
                if (enemyHealth != null)
                {
                    enemyHealth.TakeDamage(Mathf.RoundToInt(explosionDamage));
                }
            }
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw explosion radius in editor
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
} 