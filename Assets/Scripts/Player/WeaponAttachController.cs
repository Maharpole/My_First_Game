using UnityEngine;

// Attaches a weapon prefab when MainHand is equipped and configures AutoShooter
[RequireComponent(typeof(CharacterEquipment))]
public class WeaponAttachController : MonoBehaviour
{
    public Transform attachPoint; // optional; if null, uses this.transform

    private CharacterEquipment equipment;
    private GameObject currentWeaponGO;

    void Awake()
    {
        equipment = GetComponent<CharacterEquipment>();
        if (attachPoint == null) attachPoint = transform;
    }

    void OnEnable()
    {
        if (equipment != null) equipment.onEquipmentChanged.AddListener(HandleEquipmentChanged);
        HandleEquipmentChanged();
    }

    void OnDisable()
    {
        if (equipment != null) equipment.onEquipmentChanged.RemoveListener(HandleEquipmentChanged);
    }

    void HandleEquipmentChanged()
    {
        Debug.Log("[WeaponAttach] Equipment changed; updating weapon attachment");
        // Clear existing weapon instance
        if (currentWeaponGO != null)
        {
            Debug.Log("[WeaponAttach] Destroying previous weapon instance");
            Destroy(currentWeaponGO);
            currentWeaponGO = null;
        }

        var mainHand = equipment.mainHand;
        if (mainHand != null && mainHand.HasItem && mainHand.EquippedItem != null)
        {
            var item = mainHand.EquippedItem;
            if (item.isWeapon)
            {
                Debug.Log($"[WeaponAttach] MainHand weapon detected: {item.equipmentName}");
                // Prefer explicit profile on EquipmentData
                WeaponData weaponProfile = item.weaponProfile != null ? item.weaponProfile : Resources.Load<WeaponData>("WeaponProfiles/DefaultWeapon");
                // Choose prefab: EquipmentData.modelPrefab first, else WeaponData.weaponPrefab
                GameObject prefab = item.modelPrefab != null ? item.modelPrefab : (weaponProfile != null ? weaponProfile.weaponPrefab : null);
                if (prefab == null)
                {
                    Debug.LogWarning("[WeaponAttach] No model prefab on EquipmentData and no weaponPrefab on WeaponData; creating empty holder");
                    // Create an empty holder
                    currentWeaponGO = new GameObject("EquippedWeapon");
                    currentWeaponGO.transform.SetParent(attachPoint, false);
                }
                else
                {
                    currentWeaponGO = Instantiate(prefab, attachPoint);
                    currentWeaponGO.name = "EquippedWeapon";
                }

                var shooter = currentWeaponGO.GetComponent<AutoShooter>();
                if (shooter == null)
                {
                    shooter = currentWeaponGO.AddComponent<AutoShooter>();
                }

                // Configure from WeaponData if available
                if (weaponProfile != null)
                {
                    Debug.Log($"[WeaponAttach] Configuring AutoShooter from profile: {weaponProfile.weaponName}");
                    shooter.weaponData = weaponProfile;
                    shooter.damage = weaponProfile.baseDamage;
                    shooter.fireRate = weaponProfile.baseFireRate;
                    shooter.bulletSpeed = weaponProfile.baseBulletSpeed;
                    shooter.bulletCount = weaponProfile.baseBulletCount;
                    shooter.spreadAngle = weaponProfile.baseSpreadAngle;
                    shooter.bulletPrefab = weaponProfile.bulletPrefab;
                    shooter.shootSounds = weaponProfile.shootSounds;
                    shooter.shootVolume = weaponProfile.shootVolume;
                    shooter.muzzleFlash = weaponProfile.muzzleFlash;
                    shooter.impactEffect = weaponProfile.impactEffect;
                }
                else
                {
                    Debug.LogWarning("[WeaponAttach] No WeaponData profile found; AutoShooter will use defaults");
                }
            }
            else
            {
                Debug.Log("[WeaponAttach] MainHand item is not marked as weapon; skipping attach");
            }
        }
        else
        {
            Debug.Log("[WeaponAttach] No MainHand item equipped");
        }
    }
}


