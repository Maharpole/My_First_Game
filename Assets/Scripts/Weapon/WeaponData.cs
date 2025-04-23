using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "Game/Weapons/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [System.Serializable]
    public class WeaponUpgrade
    {
        public string upgradeName;
        public string description;
        public int cost;
        public float damageMultiplier = 1f;
        public float fireRateMultiplier = 1f;
        public float bulletSpeedMultiplier = 1f;
        public int bulletCount = 1;
        public float spreadAngle = 0f;
    }

    [Header("Basic Settings")]
    public string weaponName;
    public GameObject weaponPrefab;
    public GameObject bulletPrefab;
    public Sprite weaponIcon;
    
    [Header("Default Stats")]
    public float baseDamage = 10f;
    public float baseFireRate = 1f;
    public float baseBulletSpeed = 20f;
    public int baseBulletCount = 1;
    public float baseSpreadAngle = 0f;
    
    [Header("Upgrades")]
    public WeaponUpgrade[] availableUpgrades;
    
    [Header("Audio")]
    public AudioClip[] shootSounds;
    public float shootVolume = 1f;
    
    [Header("Visual Effects")]
    public ParticleSystem muzzleFlash;
    public ParticleSystem impactEffect;
} 