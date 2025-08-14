using UnityEngine;

public class HomingBullet : Bullet
{
    [Header("Homing Settings")]
    public float turnSpeed = 5f;
    public float maxTurnAngle = 45f;
    public float homingRange = 10f;
    public float maxSpeed = 30f;
    public float acceleration = 10f;
    
    private Transform target;
    private Rigidbody rb;
    private float currentSpeed;
    
    protected override void Start()
    {
        base.Start();
        
        rb = GetComponent<Rigidbody>();
        currentSpeed = rb.linearVelocity.magnitude;
    }
    
    void Update()
    {
        if (target == null)
        {
            // Find a new target
            FindTarget();
        }
        
        if (target != null)
        {
            // Calculate direction to target
            Vector3 directionToTarget = (target.position - transform.position).normalized;
            
            // Calculate current direction
            Vector3 currentDirection = rb.linearVelocity.normalized;
            
            // Calculate angle between current direction and target direction
            float angle = Vector3.Angle(currentDirection, directionToTarget);
            
            // Only turn if within max turn angle
            if (angle <= maxTurnAngle)
            {
                // Smoothly rotate towards target
                Vector3 newDirection = Vector3.Lerp(currentDirection, directionToTarget, turnSpeed * Time.deltaTime).normalized;
                
                // Apply new direction
                rb.linearVelocity = newDirection * currentSpeed;
                
                // Rotate bullet to face direction
                transform.rotation = Quaternion.LookRotation(newDirection);
            }
            
            // Accelerate
            currentSpeed = Mathf.Min(currentSpeed + acceleration * Time.deltaTime, maxSpeed);
            rb.linearVelocity = rb.linearVelocity.normalized * currentSpeed;
        }
    }
    
    void FindTarget()
    {
        // Find all enemies in range
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        float nearestDistance = homingRange;
        
        foreach (GameObject enemy in enemies)
        {
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                target = enemy.transform;
            }
        }
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
            
            // Destroy the bullet
            Destroy(gameObject);
        }
    }
} 