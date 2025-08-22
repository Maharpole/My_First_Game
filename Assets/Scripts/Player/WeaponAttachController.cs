using UnityEngine;

// Attaches a weapon prefab when MainHand is equipped and configures AutoShooter
[RequireComponent(typeof(CharacterEquipment))]
public class WeaponAttachController : MonoBehaviour
{
    public Transform attachPoint; // optional; if null, uses this.transform
    [Header("Alignment")]
    [Tooltip("If true, zero the weapon root's local transform under the attachPoint.")]
    public bool zeroLocalOnAttach = true;
    [Tooltip("If set, align this child transform of the weapon (e.g., 'Controller' or 'Handle') to the attachPoint by canceling its local offset/rotation.")]
    public string weaponAnchorChildName = "Controller";

    private CharacterEquipment equipment;
    private GameObject currentWeaponGO;

    void Awake()
    {
        equipment = GetComponent<CharacterEquipment>();
        if (attachPoint == null || (attachPoint != null && !attachPoint.gameObject.scene.IsValid()))
        {
            // Ensure we never reference a prefab asset transform as parent
            attachPoint = transform;
        }
    }

    void OnEnable()
    {
        if (equipment != null) equipment.onEquipmentChanged.AddListener(HandleEquipmentChanged);
    }

    void Start()
    {
        // Defer initial attach until after Player.InitializePlayer has run
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
                // Choose prefab directly from EquipmentData
                GameObject prefab = item.modelPrefab;
                if (prefab == null)
                {
                    Debug.LogWarning("[WeaponAttach] No model prefab on EquipmentData; creating empty holder");
                    // Create an empty holder
                    currentWeaponGO = new GameObject("EquippedWeapon");
                    currentWeaponGO.transform.SetParent(attachPoint, false);
                }
                else
                {
                    // Instantiate without a persistent parent, then parent to a valid scene transform
                    Transform parentForInstance = (attachPoint != null && attachPoint.gameObject.scene.IsValid()) ? attachPoint : transform;
                    currentWeaponGO = Instantiate(prefab, parentForInstance.position, parentForInstance.rotation);
                    currentWeaponGO.name = "EquippedWeapon";
                    var root = currentWeaponGO.transform;
                    root.SetParent(parentForInstance, false);

                    // Optional: zero local to place root exactly at the attach point
                    if (zeroLocalOnAttach)
                    {
                        root.localPosition = Vector3.zero;
                        root.localRotation = Quaternion.identity;
                        root.localScale = Vector3.one;
                    }

                    // Optional: align a specific child anchor (e.g., 'Controller') to the attach point
                    if (!string.IsNullOrEmpty(weaponAnchorChildName))
                    {
                        var anchor = root.GetComponentInChildren<Transform>(true);
                        // Find by name manually to avoid grabbing root
                        anchor = WeaponAttachUtil.FindChildByName(root, weaponAnchorChildName);
                        if (anchor != null && anchor != root)
                        {
                            // First neutralize the anchor's local rotation so it becomes identity at the attach point
                            root.localRotation = root.localRotation * Quaternion.Inverse(anchor.localRotation);
                            // Recompute anchor local after rotation, then cancel its local position
                            Vector3 anchorLocal = anchor.localPosition;
                            root.localPosition = root.localPosition - anchorLocal;
                        }
                    }

                    // Hide visual renderers so the weapon model is not shown
                    var meshRenderers = root.GetComponentsInChildren<MeshRenderer>(true);
                    for (int i = 0; i < meshRenderers.Length; i++) meshRenderers[i].enabled = false;
                    var skinnedRenderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                    for (int i = 0; i < skinnedRenderers.Length; i++) skinnedRenderers[i].enabled = false;
                }

                var shooter = currentWeaponGO.GetComponent<AutoShooter>();
                if (shooter == null)
                {
                    shooter = currentWeaponGO.AddComponent<AutoShooter>();
                }
                // Treat AutoShooter component on the prefab as the single source of weapon stats
                shooter.SetBaselines(shooter.damage, shooter.fireRate);
                // Force bullets to spawn from the attachment point
                if (attachPoint != null) shooter.muzzlePoint = attachPoint;
                // Apply player stat modifiers immediately after attaching
                var player = GetComponent<Player>();
                if (player != null)
                {
                    player.RecomputeAndApplyStats();
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

static class WeaponAttachUtil
{
    public static Transform FindChildByName(Transform root, string name)
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
}


