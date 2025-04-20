using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    [Tooltip("How long the bullet lives before being destroyed")]
    public float lifetime = 3f;
    
    [Tooltip("How much damage this bullet deals")]
    public int damage = 3;

    private void Awake()
    {
        // Set the bullet to the "Bullet" layer
        gameObject.layer = LayerMask.NameToLayer("Bullet");
    }

    void Start()
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
    
    void OnTriggerEnter(Collider other)
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
                enemyHealth.TakeDamage(damage);
            }
            
            // Destroy the bullet
            Destroy(gameObject);
        }
    }
} 