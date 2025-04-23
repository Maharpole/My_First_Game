using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

public class WeaponManager : MonoBehaviour
{
    [Header("Weapon Settings")]
    public WeaponData[] availableWeapons;
    
    [Header("Weapon Positioning")]
    [Tooltip("Distance from player to weapons (in Unity units)")]
    [Range(0.1f, 2f)]
    public float weaponDistance = 0.3f;
    
    [Tooltip("Number of positions around the player")]
    [Range(4, 12)]
    public int weaponPositions = 8;
    
    [Tooltip("Base rotation speed (degrees per second)")]
    [Range(10f, 360f)]
    public float baseRotationSpeed = 180f;
    
    [Tooltip("Maximum rotation speed multiplier for large turns")]
    [Range(1f, 5f)]
    public float maxRotationMultiplier = 3f;
    
    [Tooltip("Angle threshold (in degrees) where rotation speed starts to increase")]
    [Range(10f, 90f)]
    public float angleThreshold = 45f;
    
    [Tooltip("Fix backwards weapons by rotating them 180 degrees")]
    public bool fixBackwardsWeapons = true;
    
    [Header("Economy")]
    public int currentCoins = 0;
    
    [Header("Events")]
    public UnityEvent<int> onCoinsChanged;
    public UnityEvent<WeaponData> onWeaponAdded;
    public UnityEvent<WeaponData.WeaponUpgrade> onUpgradePurchased;
    
    private Dictionary<WeaponData, List<WeaponData.WeaponUpgrade>> purchasedUpgrades = new Dictionary<WeaponData, List<WeaponData.WeaponUpgrade>>();
    private List<GameObject> activeWeaponInstances = new List<GameObject>();
    private GameObject player;
    private Vector3 playerMovementDirection = Vector3.forward;
    private bool isInitialized = false;
    
    void Start()
    {
        // Initialize upgrades dictionary
        foreach (var weapon in availableWeapons)
        {
            purchasedUpgrades[weapon] = new List<WeaponData.WeaponUpgrade>();
        }
        
        // Try to find the player
        FindPlayer();
    }
    
    void Update()
    {
        // If not initialized, try to find the player
        if (!isInitialized)
        {
            FindPlayer();
            return;
        }
        
        // Update player movement direction
        UpdatePlayerMovementDirection();
        
        // Update weapon rotations
        UpdateWeaponRotations();
    }
    
    private void FindPlayer()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                isInitialized = true;
                Debug.Log("Player found and WeaponManager initialized");
            }
        }
    }
    
    private void UpdatePlayerMovementDirection()
    {
        if (player == null)
        {
            FindPlayer();
            return;
        }
        
        // Get player's movement direction from input or velocity
        // This assumes the player has a Rigidbody or CharacterController
        Rigidbody playerRb = player.GetComponent<Rigidbody>();
        if (playerRb != null && playerRb.velocity.magnitude > 0.1f)
        {
            playerMovementDirection = playerRb.velocity.normalized;
        }
        else
        {
            // If no velocity, try to get direction from input
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            
            if (Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f)
            {
                playerMovementDirection = new Vector3(horizontal, 0, vertical).normalized;
            }
        }
    }
    
    private void UpdateWeaponRotations()
    {
        foreach (var weaponInstance in activeWeaponInstances)
        {
            if (weaponInstance == null) continue;
            
            AutoShooter shooter = weaponInstance.GetComponent<AutoShooter>();
            if (shooter == null) continue;
            
            // Try to find a target
            GameObject target = FindNearestEnemy(weaponInstance.transform.position, shooter.detectionRange);
            
            Vector3 targetDirection;
            if (target != null)
            {
                // Face the target
                targetDirection = (target.transform.position - weaponInstance.transform.position).normalized;
            }
            else
            {
                // Face the player's movement direction
                targetDirection = playerMovementDirection;
            }
            
            // Update weapon rotation smoothly
            if (targetDirection != Vector3.zero)
            {
                // Calculate the target rotation
                Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
                
                // Apply 180-degree rotation if needed to fix backwards weapons
                if (fixBackwardsWeapons)
                {
                    targetRotation *= Quaternion.Euler(0, 180, 0);
                }
                
                // Calculate the angle difference between current and target rotation
                float angleDifference = Quaternion.Angle(weaponInstance.transform.rotation, targetRotation);
                
                // Calculate dynamic rotation speed based on angle difference
                float dynamicRotationSpeed = CalculateDynamicRotationSpeed(angleDifference);
                
                // Smoothly rotate towards the target rotation
                weaponInstance.transform.rotation = Quaternion.RotateTowards(
                    weaponInstance.transform.rotation, 
                    targetRotation, 
                    dynamicRotationSpeed * Time.deltaTime
                );
            }
        }
    }
    
    private float CalculateDynamicRotationSpeed(float angleDifference)
    {
        // If angle difference is below threshold, use base speed
        if (angleDifference <= angleThreshold)
        {
            return baseRotationSpeed;
        }
        
        // Calculate multiplier based on angle difference
        // The further from threshold, the higher the multiplier (up to maxRotationMultiplier)
        float normalizedAngle = Mathf.Clamp01((angleDifference - angleThreshold) / (180f - angleThreshold));
        float multiplier = 1f + (normalizedAngle * (maxRotationMultiplier - 1f));
        
        return baseRotationSpeed * multiplier;
    }
    
    private GameObject FindNearestEnemy(Vector3 position, float maxRange)
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject nearestEnemy = null;
        float nearestDistance = maxRange;
        
        foreach (GameObject enemy in enemies)
        {
            float distance = Vector3.Distance(position, enemy.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestEnemy = enemy;
            }
        }
        
        return nearestEnemy;
    }
    
    public void AddWeapon(int index)
    {
        if (index < 0 || index >= availableWeapons.Length) return;
        
        WeaponData weaponData = availableWeapons[index];
        
        // Check if weapon is already active
        foreach (var instance in activeWeaponInstances)
        {
            AutoShooter autoShooter = instance.GetComponent<AutoShooter>();
            if (autoShooter != null && autoShooter.weaponData == weaponData)
            {
                Debug.Log($"Weapon {weaponData.weaponName} is already active!");
                return;
            }
        }
        
        // Find the player GameObject if not already found
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogError("Player not found! Make sure your player has the 'Player' tag.");
                return;
            }
        }
        
        // Create new weapon and parent it to the player
        GameObject weaponInstance = Instantiate(weaponData.weaponPrefab, player.transform);
        
        // Calculate position based on number of active weapons
        int weaponCount = activeWeaponInstances.Count;
        float angle = (360f / weaponPositions) * weaponCount; // Divide circle into weaponPositions
        float radians = angle * Mathf.Deg2Rad;
        
        // Calculate position using trigonometry
        float x = Mathf.Cos(radians) * weaponDistance;
        float z = Mathf.Sin(radians) * weaponDistance;
        
        // Set position and rotation
        weaponInstance.transform.localPosition = new Vector3(x, 0f, z);
        
        // Initial rotation will be set in UpdateWeaponRotations
        weaponInstance.transform.rotation = Quaternion.identity;
        
        // Apply initial 180-degree rotation if needed to fix backwards weapons
        if (fixBackwardsWeapons)
        {
            weaponInstance.transform.rotation = Quaternion.Euler(0, 180, 0);
        }
        
        AutoShooter shooter = weaponInstance.GetComponent<AutoShooter>();
        
        if (shooter != null)
        {
            // Set the weapon data reference
            shooter.weaponData = weaponData;
            
            // Apply upgrades
            ApplyUpgrades(weaponData, shooter);
            
            // Add to active weapons
            activeWeaponInstances.Add(weaponInstance);
            
            // Notify listeners
            onWeaponAdded.Invoke(weaponData);
            
            Debug.Log($"Added weapon: {weaponData.weaponName} to player at position {weaponInstance.transform.localPosition} with distance {weaponDistance}");
        }
    }
    
    public void AddCoins(int amount)
    {
        currentCoins += amount;
        onCoinsChanged.Invoke(currentCoins);
    }
    
    public bool CanAffordUpgrade(WeaponData.WeaponUpgrade upgrade)
    {
        return currentCoins >= upgrade.cost;
    }
    
    public bool PurchaseUpgrade(WeaponData weapon, WeaponData.WeaponUpgrade upgrade)
    {
        if (!CanAffordUpgrade(upgrade)) return false;
        
        currentCoins -= upgrade.cost;
        purchasedUpgrades[weapon].Add(upgrade);
        
        // Apply the upgrade to all instances of this weapon
        foreach (var instance in activeWeaponInstances)
        {
            AutoShooter shooter = instance.GetComponent<AutoShooter>();
            if (shooter != null && shooter.weaponData == weapon)
            {
                ApplyUpgrades(weapon, shooter);
            }
        }
        
        onCoinsChanged.Invoke(currentCoins);
        onUpgradePurchased.Invoke(upgrade);
        
        return true;
    }
    
    private void ApplyUpgrades(WeaponData weapon, AutoShooter shooter)
    {
        if (shooter == null) return;
        
        // Reset to base stats
        shooter.damage = weapon.baseDamage;
        shooter.fireRate = weapon.baseFireRate;
        shooter.bulletSpeed = weapon.baseBulletSpeed;
        shooter.bulletCount = weapon.baseBulletCount;
        shooter.spreadAngle = weapon.baseSpreadAngle;
        
        // Apply all purchased upgrades
        foreach (var upgrade in purchasedUpgrades[weapon])
        {
            shooter.damage *= upgrade.damageMultiplier;
            shooter.fireRate *= upgrade.fireRateMultiplier;
            shooter.bulletSpeed *= upgrade.bulletSpeedMultiplier;
            shooter.bulletCount = upgrade.bulletCount;
            shooter.spreadAngle = upgrade.spreadAngle;
        }
    }
    
    public List<WeaponData> GetActiveWeapons()
    {
        List<WeaponData> activeWeapons = new List<WeaponData>();
        
        foreach (var instance in activeWeaponInstances)
        {
            AutoShooter shooter = instance.GetComponent<AutoShooter>();
            if (shooter != null && shooter.weaponData != null)
            {
                activeWeapons.Add(shooter.weaponData);
            }
        }
        
        return activeWeapons;
    }
    
    public List<WeaponData.WeaponUpgrade> GetPurchasedUpgrades(WeaponData weapon)
    {
        return purchasedUpgrades[weapon];
    }
} 