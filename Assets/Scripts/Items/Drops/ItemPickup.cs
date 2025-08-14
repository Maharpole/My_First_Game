using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class ItemPickup : MonoBehaviour, IPointerClickHandler
{
    public float rotateSpeed = 45f;

    [Header("Label")]
    public bool showNameLabel = true;
    public float nameHeight = 0.8f;
    public Color commonColor = new Color(1f,1f,1f,1f);
    public Color magicColor = new Color(0.4f,0.6f,1f,1f);
    public Color rareColor = new Color(1f,0.84f,0f,1f);
    public int nameFontSize = 24;

    private TextMeshPro nameText;
    private RuntimeEquipmentItem runtime;
    
    [Header("Visual Scale")]
    [Tooltip("If true, scales the spawned visual so its largest dimension matches targetVisualSize in world units.")]
    public bool normalizeVisualScale = true;
    [Tooltip("Target world-space size for the largest dimension of the visual's bounds (meters).")]
    public float targetVisualSize = 0.6f;
    [Tooltip("Clamp for the scaling multiplier applied during normalization.")]
    public float minScaleMultiplier = 0.05f;
    public float maxScaleMultiplier = 20f;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void Awake()
    {
        runtime = GetComponent<RuntimeEquipmentItem>();
        if (showNameLabel)
        {
            CreateNameLabel();
        }
        EnsureUIClickability();
    }

    void Start()
    {
        // After instantiation, EquipmentDropper assigns runtime.generated. Start runs after that.
        if (!visualSpawned && runtime != null && runtime.generated != null && runtime.generated.baseEquipment != null)
        {
            TrySetupDropVisual();
            visualSpawned = true;
        }
    }

    void Update()
    {
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
        UpdateNameLabel();
        // Late assignment safety: if generated was set after Start for any reason
        if (!visualSpawned && runtime != null && runtime.generated != null && runtime.generated.baseEquipment != null)
        {
            TrySetupDropVisual();
            visualSpawned = true;
        }
    }

    bool visualSpawned = false;

    void TrySetupDropVisual()
    {
        // Avoid double-spawn if already created
        if (transform.Find("DropModel") != null) return;
        if (runtime == null || runtime.generated == null || runtime.generated.baseEquipment == null) return;
        var baseItem = runtime.generated.baseEquipment;
        // Prefer dropModelPrefab; else dropMesh; else base modelPrefab
        GameObject vis = null;
        if (baseItem.dropModelPrefab != null)
        {
            vis = Instantiate(baseItem.dropModelPrefab, transform);
        }
        else if (baseItem.dropMesh != null)
        {
            var go = new GameObject("DropModel");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = baseItem.dropMesh;
            var mr = go.AddComponent<MeshRenderer>();
            if (baseItem.dropMeshMaterial != null) mr.sharedMaterial = baseItem.dropMeshMaterial;
            else if (baseItem.modelPrefab != null)
            {
                var srcMR = baseItem.modelPrefab.GetComponentInChildren<MeshRenderer>();
                if (srcMR != null) mr.sharedMaterial = srcMR.sharedMaterial;
            }
            vis = go;
        }
        else if (baseItem.modelPrefab != null)
        {
            vis = Instantiate(baseItem.modelPrefab, transform);
        }
        if (vis == null)
        {
            vis = CreateFallbackVisual(baseItem);
            if (vis == null) return;
        }
        vis.name = "DropModel";
        vis.transform.localPosition = Vector3.zero;
        vis.transform.localRotation = Quaternion.identity;
        vis.transform.localScale = Vector3.one;

        // Ensure collider remains on root; disable colliders on child visual to avoid double triggers
        foreach (var c in vis.GetComponentsInChildren<Collider>())
        {
            if (c == null) continue;
            // Never touch our root collider
            if (c.gameObject == this.gameObject) continue;
            // If a child mistakenly has ItemPickup, don't remove its required collider; just disable
            c.enabled = false;
        }

        // Normalize size so all drops appear consistent
        if (normalizeVisualScale)
        {
            NormalizeVisualScale(vis);
        }
    }

    GameObject CreateFallbackVisual(EquipmentData baseItem)
    {
        // Try quad with icon sprite as texture
        if (baseItem != null && baseItem.icon != null && baseItem.icon.texture != null)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.transform.SetParent(transform);
            quad.transform.localPosition = Vector3.zero;
            quad.transform.localRotation = Quaternion.identity;
            quad.transform.localScale = Vector3.one * 0.6f;

            var mr = quad.GetComponent<MeshRenderer>();
            var shader = Shader.Find("Unlit/Texture");
            if (shader != null)
            {
                var mat = new Material(shader);
                mat.mainTexture = baseItem.icon.texture;
                mr.sharedMaterial = mat;
            }
            // Face upward slightly so it is visible from above
            quad.transform.Rotate(Vector3.right, 90f, Space.Self);
            return quad;
        }

        // Fallback: small colored cube using rarity color
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.SetParent(transform);
        cube.transform.localPosition = Vector3.zero;
        cube.transform.localRotation = Quaternion.identity;
        cube.transform.localScale = Vector3.one * 0.25f;
        var rend = cube.GetComponent<MeshRenderer>();
        if (rend != null)
        {
            var mat = new Material(Shader.Find("Standard"));
            mat.color = baseItem != null ? baseItem.rarityColor : Color.white;
            rend.sharedMaterial = mat;
        }
        return cube;
    }

    void NormalizeVisualScale(GameObject vis)
    {
        if (vis == null) return;
        var renderers = vis.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0) return;
        var bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        var size = bounds.size;
        float maxDim = Mathf.Max(size.x, size.y, size.z);
        if (maxDim <= 0.0001f) return;
        float factor = targetVisualSize / maxDim;
        factor = Mathf.Clamp(factor, minScaleMultiplier, maxScaleMultiplier);
        vis.transform.localScale = vis.transform.localScale * factor;
    }

    void LateUpdate()
    {
        if (nameText != null && Camera.main != null)
        {
            // Face the camera (match camera orientation to avoid mirrored/backwards text)
            var cam = Camera.main.transform;
            nameText.transform.rotation = Quaternion.LookRotation(cam.forward, cam.up);
        }
    }

    public void TryPickup(Player player, float maxDistance = 1.5f)
    {
        if (player == null) return;
        if ((player.transform.position - transform.position).sqrMagnitude > maxDistance * maxDistance) return;

        // Use the same logic as trigger pickup
        DoPickup(player);
    }

    void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponent<Player>();
        if (player == null)
        {
            player = other.GetComponentInParent<Player>();
        }

        if (player == null) return;
        DoPickup(player);
    }

    // Click-to-pickup via UI EventSystem raycasts
    public void OnPointerClick(PointerEventData eventData)
    {
        var player = FindObjectOfType<Player>();
        if (player == null) return;
        // Optional: range check on click
        TryPickup(player, 3f);
    }

    void DoPickup(Player player)
    {
        if (runtime == null || runtime.generated == null || runtime.generated.baseEquipment == null) return;

        // Convert GeneratedItem to a runtime EquipmentData instance with rolled affixes
        var runtimeItem = CreateRuntimeEquipment(runtime.generated);

        // Prefer putting into inventory if space exists; otherwise drop at player position
        var inv = ResolveInventory();
        int emptySlot = inv != null ? inv.FindFirstEmpty() : -1;
        if (inv != null && emptySlot >= 0)
        {
            inv.Set(emptySlot, runtimeItem);
            Destroy(gameObject);
            return;
        }

        // Inventory full: move this pickup to player location (drop at feet)
        var basePos = player.transform.position;
        transform.position = basePos + new Vector3(0f, 0.1f, 0f);
        // Optionally nudge up so it isn't inside terrain
        if (nameText != null)
        {
            nameText.transform.localPosition = Vector3.up * nameHeight;
        }
        // Keep the item in world; do not destroy
        return;
    }

    // Find the SimpleInventory data even if its UI is currently inactive
    SimpleInventory ResolveInventory()
    {
        // Fast path: active UI present
        var ui = Object.FindFirstObjectByType<SimpleInventoryUI>();
        if (ui != null && ui.inventoryData != null) return ui.inventoryData;

#if UNITY_2020_1_OR_NEWER
        // Include inactive objects (panels hidden by UIToggleHotkey)
        var uiAny = Object.FindFirstObjectByType<SimpleInventoryUI>();
        if (uiAny != null && uiAny.inventoryData != null) return uiAny.inventoryData;
#endif

        // Last resort: scan all loaded objects (avoids coupling pickup to UI visibility)
        var allUIs = Resources.FindObjectsOfTypeAll<SimpleInventoryUI>();
        for (int i = 0; i < allUIs.Length; i++)
        {
            if (allUIs[i] != null && allUIs[i].inventoryData != null)
            {
                return allUIs[i].inventoryData;
            }
        }
        return null;
    }

    void CreateNameLabel()
    {
        var go = new GameObject("ItemName");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.up * nameHeight;

        nameText = go.AddComponent<TextMeshPro>();
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.fontSize = nameFontSize;
        nameText.color = GetRarityColor();
        nameText.enableAutoSizing = false;
        nameText.text = GetDisplayName();
        nameText.raycastTarget = false; // TextMeshPro (3D) uses physics, not UI raycasts

        // Add a small collider so the label itself is clickable
        var bc = go.AddComponent<BoxCollider>();
        bc.isTrigger = false;
        bc.center = new Vector3(0f, 0f, 0f);
        bc.size = new Vector3(1.2f, 0.35f, 0.1f);

        // Relay clicks on the label up to the pickup
        var relay = go.AddComponent<ItemPickupClickRelay>();
        relay.target = this;
    }

    void UpdateNameLabel()
    {
        if (nameText == null) return;
        string desired = GetDisplayName();
        if (nameText.text != desired)
        {
            nameText.text = desired;
        }
        var desiredColor = GetRarityColor();
        if (nameText.color != desiredColor) nameText.color = desiredColor;
    }

    string GetDisplayName()
    {
        if (runtime != null && runtime.generated != null && runtime.generated.baseEquipment != null)
        {
            // Optionally prefix with rarity color
            return runtime.GetDisplayName();
        }
        return "Item";
    }

    Color GetRarityColor()
    {
        if (runtime != null && runtime.generated != null)
        {
            switch (runtime.generated.rarity)
            {
                case ItemRarity.Magic: return magicColor;
                case ItemRarity.Rare: return rareColor;
                default: return commonColor;
            }
        }
        return commonColor;
    }

    EquipmentData CreateRuntimeEquipment(GeneratedItem gen)
    {
        var baseItem = gen.baseEquipment;
        var copy = ScriptableObject.CreateInstance<EquipmentData>();
        copy.equipmentName = baseItem.equipmentName;
        copy.description = baseItem.description;
        copy.icon = baseItem.icon;
        copy.modelPrefab = baseItem.modelPrefab;
        copy.equipmentType = baseItem.equipmentType;
        copy.rarity = baseItem.rarity;
        copy.itemLevel = gen.itemLevel;
        copy.requiredLevel = baseItem.requiredLevel;
        copy.isWeapon = baseItem.isWeapon;
        copy.handUsage = baseItem.handUsage;
        copy.allowedOffhands = baseItem.allowedOffhands;
        copy.occupiesBothHands = baseItem.occupiesBothHands;
        copy.weaponProfile = baseItem.weaponProfile;
        // base stats
        copy.baseStats = new System.Collections.Generic.List<StatModifier>(baseItem.baseStats);
        // rolled stats
        var rolled = new System.Collections.Generic.List<StatModifier>();
        if (gen.prefixes != null)
            foreach (var p in gen.prefixes) rolled.Add(new StatModifier { statType = p.statType, value = p.value, isPercentage = p.isPercentage });
        if (gen.suffixes != null)
            foreach (var s in gen.suffixes) rolled.Add(new StatModifier { statType = s.statType, value = s.value, isPercentage = s.isPercentage });
        copy.SetRandomAffixes(rolled);
        // rarity color (optional: map by rarity)
        copy.rarityColor = (runtime != null && runtime.generated != null) ? copy.rarityColor : copy.rarityColor;
        return copy;
    }

    void EnsureUIClickability()
    {
        // Add a world-space physics raycaster target: require collider (present) and an EventSystem+InputModule in the scene
        var go = gameObject;
        // Ensure there is a tiny child canvas for the label that doesn’t block clicks on the item
        // Clicks target this GameObject due to IPointerClickHandler; PhysicsRaycaster on camera is needed
        var cam = Camera.main;
        if (cam != null)
        {
            if (cam.GetComponent<PhysicsRaycaster>() == null) cam.gameObject.AddComponent<PhysicsRaycaster>();
        }
    }
}
