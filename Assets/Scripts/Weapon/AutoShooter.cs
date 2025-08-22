using UnityEngine;
using System.Collections;

public class AutoShooter : MonoBehaviour
{
    [Header("Weapon Stats (Prefab-defined)")]
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
    
    [Header("Muzzle")] 
    [Tooltip("If set, bullets will spawn from this point; otherwise code will try to find a child named 'Muzzle'/'MuzzlePoint'/'BarrelEnd'.")]
    public Transform muzzlePoint;
    [Tooltip("Child name to search for if muzzlePoint is not assigned.")]
    public string muzzleChildName = "Muzzle";
    
    [Header("Sound Effects")]
    public AudioClip[] shootSounds;
    public float shootVolume = 1f;
    
    private float nextFireTime;
    private AudioSource audioSource;

    // Baselines captured from weapon profile or defaults
    private float baselineDamage;
    private float baselineFireInterval;

    // Runtime computed combat values
    private float currentDamage;
    private float currentFireInterval;
    private float currentCritChancePercent = 2f;     // default base 2%
    private float currentCritMultiplierPercent = 150f; // default base 150%
    
    void Start()
    {
        // Initialize baselines if not set yet
        if (baselineDamage <= 0f) baselineDamage = damage;
        if (baselineFireInterval <= 0f) baselineFireInterval = fireRate;
        // Initialize current values
        currentDamage = damage;
        currentFireInterval = fireRate;
        nextFireTime = Time.time + currentFireInterval;
        
        // Set up audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Ensure bullet prefab assigned via prefab
        
        if (bulletPrefab == null)
        {
            Debug.LogError("Bullet prefab not assigned to AutoShooter on " + gameObject.name);
        }
        
        // Sounds come from prefab
        
        // VFX come from prefab

        // Auto-find a muzzle point if not assigned
        if (muzzlePoint == null)
        {
            muzzlePoint = FindChildByName(transform, muzzleChildName);
            if (muzzlePoint == null)
            {
                // Try common alternates
                muzzlePoint = FindChildByName(transform, "MuzzlePoint") 
                               ?? FindChildByName(transform, "BarrelEnd")
                               ?? FindChildByName(transform, "Tip");
            }
        }
    }
    
    void Update()
    {
        // Always look for a target to keep aim updated for facing
        GameObject nearestEnemy = FindNearestEnemy();
        if (nearestEnemy != null)
        {
            Vector3 direction = (nearestEnemy.transform.position - transform.position);
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
            {
                direction.Normalize();
                var animBridgeForFacing = GetComponentInParent<PlayerAnimatorBridge>();
                if (animBridgeForFacing != null)
                {
                    animBridgeForFacing.ReportAimDirection(direction);
                }
            }
        }

        // Fire only when interval elapsed and a target exists
        if (nearestEnemy != null && Time.time >= nextFireTime)
        {
            Vector3 fireDir = (nearestEnemy.transform.position - transform.position);
            fireDir.y = 0f;
            if (fireDir.sqrMagnitude > 0.0001f)
            {
                fireDir.Normalize();
                FireBullets(fireDir);
                nextFireTime = Time.time + currentFireInterval;
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
        
        // Determine spawn position and base rotation (prefer muzzle)
        Vector3 basePos = (muzzlePoint != null) ? muzzlePoint.position : transform.position;
        Quaternion baseRot = Quaternion.LookRotation(baseDirection);

        // Aim reporting moved to Update to maintain facing between shots

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
            GameObject bullet = Instantiate(bulletPrefab, basePos, baseRot);
            
            // Set bullet's rotation to face the direction it's moving
            bullet.transform.rotation = Quaternion.LookRotation(direction);
            
            // Get or add Rigidbody to bullet
            Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
            if (bulletRb == null)
            {
                bulletRb = bullet.AddComponent<Rigidbody>();
            }
            
            // Set bullet's velocity
            bulletRb.linearVelocity = direction * bulletSpeed;
            
            // Set bullet damage
            Bullet bulletComponent = bullet.GetComponent<Bullet>();
            if (bulletComponent != null)
            {
                float dmg = currentDamage;
                // Roll crit
                if (currentCritChancePercent > 0f)
                {
                    if (Random.value < (currentCritChancePercent * 0.01f))
                    {
                        float mult = Mathf.Max(0f, currentCritMultiplierPercent) * 0.01f; // 150% -> 1.5
                        dmg *= mult;
                    }
                }
                bulletComponent.damage = dmg;
                
                // Hook for weapon-specific properties can be added here on the bullet if needed
            }
        }

        // Notify animation listeners via AnimatorBridge, if present
        var animBridge = GetComponentInParent<PlayerAnimatorBridge>();
        if (animBridge != null)
        {
            animBridge.FlagRangedFired();
        }
    }

    private Transform FindChildByName(Transform root, string name)
    {
        if (root == null || string.IsNullOrEmpty(name)) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var c = root.GetChild(i);
            var r = FindChildByName(c, name);
            if (r != null) return r;
        }
        return null;
    }

    // Called by weapon attach or player stat system to set the unmodified baseline values
    public void SetBaselines(float baseDamage, float baseFireInterval)
    {
        baselineDamage = Mathf.Max(0f, baseDamage);
        baselineFireInterval = Mathf.Max(0.01f, baseFireInterval);
        // Reset current based on baselines
        currentDamage = baselineDamage;
        currentFireInterval = baselineFireInterval;
    }

    // Applies player stat modifiers to this shooter
    public void ApplyStatModifiers(float flatDamage, float pctDamage, float pctAttackSpeed, float finalCritChancePercent, float finalCritMultiplierPercent)
    {
        // Damage: (base + flat) * (1 + %/100)
        currentDamage = (baselineDamage + flatDamage) * (1f + Mathf.Max(0f, pctDamage) * 0.01f);
        damage = currentDamage; // keep public field in sync for debugging

        // Fire interval: divide by (1 + attack speed%)
        float speedFactor = 1f + Mathf.Max(0f, pctAttackSpeed) * 0.01f;
        currentFireInterval = Mathf.Max(0.01f, baselineFireInterval / Mathf.Max(0.01f, speedFactor));
        fireRate = currentFireInterval; // keep public field in sync

        // Crit
        currentCritChancePercent = Mathf.Max(0f, finalCritChancePercent);
        currentCritMultiplierPercent = Mathf.Max(0f, finalCritMultiplierPercent);
    }
} 