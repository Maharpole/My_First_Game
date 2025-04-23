using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    [Tooltip("How long the bullet lives before being destroyed")]
    public float lifetime = 3f;
    
    [Tooltip("How much damage this bullet deals")]
    public float damage = 3f;
    
    [Tooltip("Whether this bullet is destroyed on impact")]
    public bool destroyOnImpact = true;

    private void Awake()
    {
        // Set the bullet to the "Bullet" layer
        gameObject.layer = LayerMask.NameToLayer("Bullet");
    }

    protected virtual void Start()
    {
        // Destroy bullet after lifetime
        Destroy(gameObject, lifetime);
        
        // Add a collider if one doesn't exist
        if (GetComponent<Collider>() == null)
        {
            SphereCollider collider = gameObject.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 0.2f;
        }

        // Get the Rigidbody component
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        // Configure Rigidbody for bullet physics
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }
    
    public virtual void OnTriggerEnter(Collider other)
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
            
            // Destroy the bullet if configured to do so
            if (destroyOnImpact)
            {
                Destroy(gameObject);
            }
        }
    }
} 