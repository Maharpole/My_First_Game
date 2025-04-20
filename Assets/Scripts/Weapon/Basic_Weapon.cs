using UnityEngine;
using System.Collections;

public class AutoShooter : MonoBehaviour
{
    [Header("Shooting Settings")]
    [Tooltip("How often the shooter fires (in seconds)")]
    public float fireRate = 1.0f;
    
    [Tooltip("The bullet prefab to instantiate")]
    public GameObject bulletPrefab;
    
    [Tooltip("The speed of the bullet")]
    public float bulletSpeed = 20f;
    
    [Tooltip("The maximum distance to detect enemies")]
    public float detectionRange = 20f;
    
    [Header("Sound Effects")]
    [Tooltip("List of possible shooting sounds to randomly select from")]
    public AudioClip[] shootSounds;
    
    [Tooltip("Volume of the shooting sound (0-1)")]
    [Range(0f, 1f)]
    public float shootVolume = 1f;
    
    private float nextFireTime;
    private AudioSource audioSource;
    
    void Start()
    {
        nextFireTime = Time.time + fireRate;
        
        // Set up audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Check if bullet prefab is assigned
        if (bulletPrefab == null)
        {
            Debug.LogError("Bullet prefab not assigned to AutoShooter on " + gameObject.name);
        }
        else
        {
            Debug.Log("AutoShooter initialized on " + gameObject.name + " with bullet prefab: " + bulletPrefab.name);
        }
    }
    
    void Update()
    {
        // Check if it's time to fire
        if (Time.time >= nextFireTime)
        {
            // Find the nearest enemy
            GameObject nearestEnemy = FindNearestEnemy();
            
            if (nearestEnemy != null)
            {
                Debug.Log("Found enemy at distance: " + Vector3.Distance(transform.position, nearestEnemy.transform.position));
                
                // Calculate direction to enemy
                Vector3 direction = (nearestEnemy.transform.position - transform.position).normalized;
                
                // Create and fire bullet
                FireBullet(direction);
                
                // Set next fire time
                nextFireTime = Time.time + fireRate;
            }
            else
            {
                Debug.Log("No enemies found within range");
            }
        }
    }
    
    GameObject FindNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Debug.Log("Found " + enemies.Length + " enemies with 'Enemy' tag");
        
        GameObject nearestEnemy = null;
        float nearestDistance = Mathf.Infinity;
        
        foreach (GameObject enemy in enemies)
        {
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            Debug.Log("Enemy at distance: " + distance);
            if (distance < nearestDistance && distance <= detectionRange)
            {
                nearestDistance = distance;
                nearestEnemy = enemy;
            }
        }
        
        return nearestEnemy;
    }
    
    void FireBullet(Vector3 direction)
    {
        // Play random shooting sound
        if (shootSounds != null && shootSounds.Length > 0 && audioSource != null)
        {
            int randomIndex = Random.Range(0, shootSounds.Length);
            audioSource.PlayOneShot(shootSounds[randomIndex], shootVolume);
        }
        
        // Create bullet
        GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        Debug.Log("Fired bullet from position: " + transform.position);
        
        // Set bullet's rotation to face the direction it's moving
        bullet.transform.rotation = Quaternion.LookRotation(direction);
        
        // Get or add Rigidbody to bullet
        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
        if (bulletRb == null)
        {
            bulletRb = bullet.AddComponent<Rigidbody>();
            Debug.Log("Added Rigidbody to bullet");
        }
        
        // Set bullet's velocity
        bulletRb.velocity = direction * bulletSpeed;
        Debug.Log("Bullet velocity set to: " + bulletRb.velocity);
        
        // Optional: Add a script to the bullet to handle collision and destruction
        if (bullet.GetComponent<Bullet>() == null)
        {
            bullet.AddComponent<Bullet>();
            Debug.Log("Added Bullet script to bullet");
        }
    }
} 