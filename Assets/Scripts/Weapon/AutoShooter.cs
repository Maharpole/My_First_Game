using UnityEngine;
using System.Collections;

public class AutoShooter : MonoBehaviour
{
    [Header("Weapon Data")]
    public WeaponData weaponData;
    
    [Header("Shooting Settings")]
    public float damage = 10f;
    public float fireRate = 1.0f;
    public float bulletSpeed = 20f;
    public int bulletCount = 1;
    public float spreadAngle = 0f;
    public float detectionRange = 20f;
    
    [Header("References")]
    public GameObject bulletPrefab;
    public ParticleSystem muzzleFlash;
    public ParticleSystem impactEffect;
    
    [Header("Sound Effects")]
    public AudioClip[] shootSounds;
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
        if (bulletPrefab == null && weaponData != null)
        {
            bulletPrefab = weaponData.bulletPrefab;
        }
        
        if (bulletPrefab == null)
        {
            Debug.LogError("Bullet prefab not assigned to AutoShooter on " + gameObject.name);
        }
        
        // Set up sound effects if not assigned
        if ((shootSounds == null || shootSounds.Length == 0) && weaponData != null && weaponData.shootSounds != null && weaponData.shootSounds.Length > 0)
        {
            shootSounds = weaponData.shootSounds;
            shootVolume = weaponData.shootVolume;
        }
        
        // Set up visual effects if not assigned
        if (muzzleFlash == null && weaponData != null && weaponData.muzzleFlash != null)
        {
            muzzleFlash = weaponData.muzzleFlash;
        }
        
        if (impactEffect == null && weaponData != null && weaponData.impactEffect != null)
        {
            impactEffect = weaponData.impactEffect;
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
                // Calculate direction to enemy
                Vector3 direction = (nearestEnemy.transform.position - transform.position).normalized;
                
                // Fire bullets
                FireBullets(direction);
                
                // Set next fire time
                nextFireTime = Time.time + fireRate;
            }
        }
    }
    
    GameObject FindNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject nearestEnemy = null;
        float nearestDistance = Mathf.Infinity;
        
        foreach (GameObject enemy in enemies)
        {
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance < nearestDistance && distance <= detectionRange)
            {
                nearestDistance = distance;
                nearestEnemy = enemy;
            }
        }
        
        return nearestEnemy;
    }
    
    void FireBullets(Vector3 baseDirection)
    {
        // Play random shooting sound
        if (shootSounds != null && shootSounds.Length > 0 && audioSource != null)
        {
            int randomIndex = Random.Range(0, shootSounds.Length);
            audioSource.PlayOneShot(shootSounds[randomIndex], shootVolume);
        }
        
        // Play muzzle flash
        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }
        
        // Calculate spread angles
        float angleStep = spreadAngle / (bulletCount > 1 ? bulletCount - 1 : 1);
        float currentAngle = -spreadAngle / 2f;
        
        // Fire multiple bullets if needed
        for (int i = 0; i < bulletCount; i++)
        {
            // Calculate spread direction
            Vector3 direction = baseDirection;
            if (spreadAngle > 0)
            {
                direction = Quaternion.Euler(0, currentAngle, 0) * direction;
                currentAngle += angleStep;
            }
            
            // Create bullet
            GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
            
            // Set bullet's rotation to face the direction it's moving
            bullet.transform.rotation = Quaternion.LookRotation(direction);
            
            // Get or add Rigidbody to bullet
            Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
            if (bulletRb == null)
            {
                bulletRb = bullet.AddComponent<Rigidbody>();
            }
            
            // Set bullet's velocity
            bulletRb.velocity = direction * bulletSpeed;
            
            // Set bullet damage
            Bullet bulletComponent = bullet.GetComponent<Bullet>();
            if (bulletComponent != null)
            {
                bulletComponent.damage = damage;
                
                // Set weapon-specific properties
                if (weaponData != null)
                {
                    // You can add special properties here based on weapon type
                    // For example, piercing bullets, explosive bullets, etc.
                }
            }
        }
    }
} 