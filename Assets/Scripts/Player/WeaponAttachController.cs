using UnityEngine;

// Attaches weapon prefabs to hand sockets based on equipped items and updates animator stance
[ExecuteAlways]
[RequireComponent(typeof(CharacterEquipment))]
public class WeaponAttachController : MonoBehaviour
{
    [Header("Attach Sockets")]
    [Tooltip("Right hand socket/bone for main-hand weapons")] public Transform rightHand;
    [Tooltip("Left hand socket/bone for off-hand or dual wield")] public Transform leftHand;
    [Tooltip("Fallback attach (used only if a hand is not assigned)")] public Transform attachPoint; // optional; if null, uses this.transform
    [Header("Alignment")]
    [Tooltip("If true, zero the weapon root's local transform under the attachPoint.")]
    public bool zeroLocalOnAttach = true;
    [Tooltip("If set, align this child transform of the weapon (e.g., 'Controller' or 'Handle') to the attachPoint by canceling its local offset/rotation.")]
    public string weaponAnchorChildName = "Controller";
    [Tooltip("Optional name of a muzzle child under the weapon prefab (sets AutoShooter.muzzlePoint)")] public string muzzleChildName = "Muzzle";

    [Header("Alignment (Advanced)")]
    [Tooltip("If true and an alignment Transform is provided, the weapon root will match that world pose while being parented to the hand bone.")]
    public bool useAlignmentTransforms = true;
    [Tooltip("World-space reference Transform representing the desired pose for the MAIN-HAND weapon root.")]
    public Transform mainHandAlignment;
    [Tooltip("World-space reference Transform representing the desired pose for the OFF-HAND weapon root.")]
    public Transform offHandAlignment;
    [Tooltip("Copy alignment reference scale to the weapon root (otherwise keep weapon's local scale=1)")]
    public bool copyAlignmentScale = false;

    [Header("Animator Stance")]
    public Animator animator;
    [Tooltip("Animator int parameter that selects upper-body stance (0=None,1=OneHand,2=TwoHand,3=DualWield)")] public string stanceParam = "WeaponStance";

    public enum WeaponStance { None = 0, OneHand = 1, TwoHand = 2, DualWield = 3 }

    private CharacterEquipment equipment;
    private GameObject currentWeaponGO;
    private GameObject currentOffhandGO;

    [Header("Live Preview")]
    [Tooltip("Re-attach automatically when sockets or settings change (helpful while tuning)")] public bool livePreview = true;
    [Tooltip("Also preview in Edit mode (not playing). If off, only works during Play mode")] public bool previewInEditMode = false;
    [Tooltip("Detect socket transform changes using Transform.hasChanged each frame")] public bool detectSocketMoves = true;

    private string _cachedAnchorName;
    private string _cachedMuzzleName;
    private Transform _currentMainAnchor;
    private Transform _currentOffAnchor;

    void Awake()
    {
        equipment = GetComponent<CharacterEquipment>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
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

    void Update()
    {
        if (!livePreview) return;
        if (!Application.isPlaying && !previewInEditMode) return;

        bool socketsMoved = false;
        if (detectSocketMoves)
        {
            if (rightHand != null && rightHand.hasChanged) { socketsMoved = true; rightHand.hasChanged = false; }
            if (leftHand != null && leftHand.hasChanged) { socketsMoved = true; leftHand.hasChanged = false; }
        }

        // Detect property changes that affect alignment
        bool settingsChanged = _cachedAnchorName != weaponAnchorChildName || _cachedMuzzleName != muzzleChildName;
        if (settingsChanged)
        {
            _cachedAnchorName = weaponAnchorChildName;
            _cachedMuzzleName = muzzleChildName;
        }

        if (socketsMoved || settingsChanged)
        {
            // Re-apply alignment without destroying/spawning if possible
            RealignCurrentInstances();
        }

        // Also watch grip anchors for manual edits in the prefab instance
        bool anchorsChanged = false;
        if (currentWeaponGO != null)
        {
            var a = string.IsNullOrEmpty(weaponAnchorChildName) ? null : WeaponAttachUtil.FindChildByName(currentWeaponGO.transform, weaponAnchorChildName);
            if (_currentMainAnchor != a) { _currentMainAnchor = a; anchorsChanged = true; }
            else if (a != null && a.hasChanged) { anchorsChanged = true; a.hasChanged = false; }
        }
        if (currentOffhandGO != null)
        {
            var a = string.IsNullOrEmpty(weaponAnchorChildName) ? null : WeaponAttachUtil.FindChildByName(currentOffhandGO.transform, weaponAnchorChildName);
            if (_currentOffAnchor != a) { _currentOffAnchor = a; anchorsChanged = true; }
            else if (a != null && a.hasChanged) { anchorsChanged = true; a.hasChanged = false; }
        }
        if (anchorsChanged)
        {
            RealignCurrentInstances();
        }
    }

    void OnDisable()
    {
        if (equipment != null) equipment.onEquipmentChanged.RemoveListener(HandleEquipmentChanged);
        // Clean up preview instances in Edit Mode to avoid duplicates next enable
        if (!Application.isPlaying && previewInEditMode)
        {
            if (currentWeaponGO != null) { SafeDestroy(currentWeaponGO); currentWeaponGO = null; }
            if (currentOffhandGO != null) { SafeDestroy(currentOffhandGO); currentOffhandGO = null; }
            // Also clear any leftover children by name under sockets
            ClearExistingUnder(ResolveAttachForMain(), "EquippedWeapon");
            ClearExistingUnder(ResolveAttachForOff(), "EquippedWeapon_Offhand");
        }
    }

    void HandleEquipmentChanged()
    {
        Debug.Log("[WeaponAttach] Equipment changed; updating weapon attachment");
        // Clear existing weapon instances
        if (currentWeaponGO != null) { SafeDestroy(currentWeaponGO); currentWeaponGO = null; }
        if (currentOffhandGO != null) { SafeDestroy(currentOffhandGO); currentOffhandGO = null; }
        // Also clear any leftover instances by name (handles domain reload / lost refs)
        ClearExistingUnder(ResolveAttachForMain(), "EquippedWeapon");
        ClearExistingUnder(ResolveAttachForOff(), "EquippedWeapon_Offhand");

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
                    Transform parent = ResolveAttachForMain();
                    currentWeaponGO.transform.SetParent(parent, false);
                }
                else
                {
                    // Instantiate and parent under the appropriate hand
                    Transform parentForInstance = ResolveAttachForMain();
                    currentWeaponGO = Instantiate(prefab, parentForInstance.position, parentForInstance.rotation);
                    currentWeaponGO.name = "EquippedWeapon";
                    var root = currentWeaponGO.transform;
                    root.SetParent(parentForInstance, false);
                    if (!Application.isPlaying && previewInEditMode)
                    {
                        currentWeaponGO.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild | HideFlags.DontSave;
                    }

                    // Alignment: prefer explicit alignment Transform, else zeroLocal/anchor
                    if (useAlignmentTransforms && mainHandAlignment != null)
                    {
                        ApplyAlignmentToParentSpace(root, parentForInstance, mainHandAlignment);
                    }
                    else
                    {
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
                            var anchor = WeaponAttachUtil.FindChildByName(root, weaponAnchorChildName);
                            if (anchor != null && anchor != root)
                            {
                                root.localRotation = root.localRotation * Quaternion.Inverse(anchor.localRotation);
                                Vector3 anchorLocal = anchor.localPosition;
                                root.localPosition = root.localPosition - anchorLocal;
                            }
                        }
                    }
                }

                // Route weapon muzzle to ClickShooter if present
                var clickShooter = GetComponentInParent<ClickShooter>() ?? GetComponent<ClickShooter>();
                if (clickShooter != null)
                {
                    var muzzle = WeaponAttachUtil.FindChildByName(currentWeaponGO.transform, muzzleChildName);
                    if (muzzle == null) muzzle = WeaponAttachUtil.FindChildByName(currentWeaponGO.transform, "MuzzlePoint")
                                           ?? WeaponAttachUtil.FindChildByName(currentWeaponGO.transform, "BarrelEnd")
                                           ?? WeaponAttachUtil.FindChildByName(currentWeaponGO.transform, "Tip");
                    if (muzzle != null) clickShooter.muzzle = muzzle;
                }

                // Decide stance and handle offhand for dual wield
                WeaponStance stance = WeaponStance.OneHand;
                if (item.handUsage == HandUsage.TwoHand || item.occupiesBothHands) stance = WeaponStance.TwoHand;

                if (stance == WeaponStance.OneHand)
                {
                    // Try to attach an offhand weapon if equipped
                    var off = equipment.offHand;
                    if (off != null && off.HasItem && off.EquippedItem != null && off.EquippedItem.isWeapon && off.EquippedItem.handUsage == HandUsage.OneHand)
                    {
                        var offPrefab = off.EquippedItem.modelPrefab;
                        if (offPrefab != null)
                        {
                            Transform parent = ResolveAttachForOff();
                            currentOffhandGO = Instantiate(offPrefab, parent.position, parent.rotation);
                            currentOffhandGO.name = "EquippedWeapon_Offhand";
                            var r = currentOffhandGO.transform; r.SetParent(parent, false);
                            if (!Application.isPlaying && previewInEditMode)
                            {
                                currentOffhandGO.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild | HideFlags.DontSave;
                            }
                            if (useAlignmentTransforms && offHandAlignment != null)
                            {
                                ApplyAlignmentToParentSpace(r, parent, offHandAlignment);
                            }
                            else
                            {
                                if (zeroLocalOnAttach) { r.localPosition = Vector3.zero; r.localRotation = Quaternion.identity; r.localScale = Vector3.one; }
                                if (!string.IsNullOrEmpty(weaponAnchorChildName))
                                {
                                    var anchor = WeaponAttachUtil.FindChildByName(r, weaponAnchorChildName);
                                    if (anchor != null && anchor != r)
                                    {
                                        r.localRotation = r.localRotation * Quaternion.Inverse(anchor.localRotation);
                                        Vector3 anchorLocal = anchor.localPosition; r.localPosition = r.localPosition - anchorLocal;
                                    }
                                }
                            }
                            stance = WeaponStance.DualWield;
                            // If dual wield, prefer right-hand muzzle for ClickShooter; leave offhand cosmetic
                        }
                    }
                }

                // Drive animator stance if parameter exists
                if (animator != null && !string.IsNullOrEmpty(stanceParam))
                {
                    try { animator.SetInteger(stanceParam, (int)stance); } catch { }
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
            if (animator != null && !string.IsNullOrEmpty(stanceParam)) animator.SetInteger(stanceParam, (int)WeaponStance.None);
        }
    }

    void RealignCurrentInstances()
    {
        if (currentWeaponGO != null)
        {
            Transform parentForInstance = ResolveAttachForMain();
            var root = currentWeaponGO.transform;
            if (root.parent != parentForInstance) root.SetParent(parentForInstance, false);
            if (useAlignmentTransforms && mainHandAlignment != null)
            {
                ApplyAlignmentToParentSpace(root, parentForInstance, mainHandAlignment);
            }
            else
            {
                if (zeroLocalOnAttach)
                {
                    root.localPosition = Vector3.zero;
                    root.localRotation = Quaternion.identity;
                    root.localScale = Vector3.one;
                }
                if (!string.IsNullOrEmpty(weaponAnchorChildName))
                {
                    var anchor = WeaponAttachUtil.FindChildByName(root, weaponAnchorChildName);
                    if (anchor != null && anchor != root)
                    {
                        root.localRotation = root.localRotation * Quaternion.Inverse(anchor.localRotation);
                        Vector3 anchorLocal = anchor.localPosition; root.localPosition = root.localPosition - anchorLocal;
                    }
                }
            }
        }

        if (currentOffhandGO != null)
        {
            Transform parentForInstance = ResolveAttachForOff();
            var root = currentOffhandGO.transform;
            if (root.parent != parentForInstance) root.SetParent(parentForInstance, false);
            if (useAlignmentTransforms && offHandAlignment != null)
            {
                ApplyAlignmentToParentSpace(root, parentForInstance, offHandAlignment);
            }
            else
            {
                if (zeroLocalOnAttach)
                {
                    root.localPosition = Vector3.zero;
                    root.localRotation = Quaternion.identity;
                    root.localScale = Vector3.one;
                }
                if (!string.IsNullOrEmpty(weaponAnchorChildName))
                {
                    var anchor = WeaponAttachUtil.FindChildByName(root, weaponAnchorChildName);
                    if (anchor != null && anchor != root)
                    {
                        root.localRotation = root.localRotation * Quaternion.Inverse(anchor.localRotation);
                        Vector3 anchorLocal = anchor.localPosition; root.localPosition = root.localPosition - anchorLocal;
                    }
                }
            }
        }
    }

    Transform ResolveAttachForMain()
    {
        if (rightHand != null && rightHand.gameObject.scene.IsValid()) return rightHand;
        if (attachPoint != null && attachPoint.gameObject.scene.IsValid()) return attachPoint;
        return transform;
    }

    Transform ResolveAttachForOff()
    {
        if (leftHand != null && leftHand.gameObject.scene.IsValid()) return leftHand;
        if (attachPoint != null && attachPoint.gameObject.scene.IsValid()) return attachPoint;
        return transform;
    }

    void SafeDestroy(GameObject go)
    {
        if (go == null) return;
        if (Application.isPlaying) Destroy(go);
        else DestroyImmediate(go);
    }

    void ClearExistingUnder(Transform parent, string nameContains)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            var c = parent.GetChild(i);
            if (c == null) continue;
            if (!string.IsNullOrEmpty(nameContains) && c.name.Contains(nameContains))
            {
                SafeDestroy(c.gameObject);
            }
        }
    }

    void ApplyAlignmentToParentSpace(Transform weaponRoot, Transform parentSpace, Transform alignment)
    {
        if (weaponRoot == null || parentSpace == null || alignment == null) return;
        // Convert alignment world pose to parent's local space
        weaponRoot.position = alignment.position;
        weaponRoot.rotation = alignment.rotation;
        weaponRoot.localPosition = parentSpace.InverseTransformPoint(weaponRoot.position);
        weaponRoot.localRotation = Quaternion.Inverse(parentSpace.rotation) * weaponRoot.rotation;
        if (copyAlignmentScale)
        {
            weaponRoot.localScale = alignment.lossyScale; // approximate; acceptable for preview/attachment
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


