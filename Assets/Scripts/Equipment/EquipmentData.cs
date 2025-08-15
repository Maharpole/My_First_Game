using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Equipment", menuName = "POE Game/Equipment/Equipment Data")]
public class EquipmentData : ScriptableObject
{
    [Header("Basic Information")]
    public string equipmentName;
    [TextArea(3, 5)] public string description;
    public Sprite icon;
    public GameObject modelPrefab;
    
    [Header("Equipment Properties")]
    public EquipmentType equipmentType;
    public EquipmentRarity rarity = EquipmentRarity.Common;
    public int itemLevel = 1;
    public int requiredLevel = 1;

    [Header("Weapon/Offhand (if applicable)")]
    public bool isWeapon = false;
    public HandUsage handUsage = HandUsage.OneHand; // two-hand blocks OffHand
    public OffhandCategory allowedOffhands = OffhandCategory.None; // for one-hand ranged (e.g., bow + quiver)
    public bool occupiesBothHands = false; // greatbow, staff
    [Tooltip("If this equipment is a weapon, link the WeaponData profile to configure AutoShooter.")]
    public WeaponData weaponProfile;

	[Header("Visual Prefabs")]
	[Tooltip("Optional: prefab to represent this item when dropped in the world. If not set, uses modelPrefab.")]
	public GameObject dropModelPrefab;
	[Tooltip("Optional: use a raw Mesh for the world drop visual (no prefab required). If set, a MeshFilter/MeshRenderer will be created at runtime.")]
	public Mesh dropMesh;
	[Tooltip("Material used with dropMesh. If not assigned, will try to reuse the base item's material or default material.")]
	public Material dropMeshMaterial;

    // Inventory grid sizing removed (we use fixed slots UI now)

    [Header("Base Statistics")]
    public List<StatModifier> baseStats = new List<StatModifier>();

    [Header("Affixes (Generated at Runtime)")]
    [SerializeField] private List<StatModifier> randomAffixes = new List<StatModifier>();
    // Extended info for UI (tier ranges, names)
    [SerializeField] private List<GeneratedAffix> generatedAffixes = new List<GeneratedAffix>();

    [Header("Visual Settings")]
    public Color rarityColor = Color.white;

    public List<StatModifier> AllStats
    {
        get
        {
            var allStats = new List<StatModifier>();
            allStats.AddRange(baseStats);
            allStats.AddRange(randomAffixes);
            return allStats;
        }
    }

    public bool CanEquipInSlot(EquipmentType slot)
    {
        // Regular slots must match exactly, including BodyArmour and Ring
        if (slot != EquipmentType.MainHand && slot != EquipmentType.OffHand)
        {
            return equipmentType == slot;
        }
        // Hand logic
        if (!isWeapon && equipmentType != EquipmentType.OffHand) return false;
        if (isWeapon && (handUsage == HandUsage.TwoHand || occupiesBothHands)) return slot == EquipmentType.MainHand;
        if (slot == EquipmentType.MainHand) return isWeapon && handUsage == HandUsage.OneHand;
        if (slot == EquipmentType.OffHand) return !isWeapon || equipmentType == EquipmentType.OffHand;
        return false;
    }

    public void SetRandomAffixes(List<StatModifier> affixes) { randomAffixes = affixes; }
    public void SetGeneratedAffixes(List<GeneratedAffix> affixes) { generatedAffixes = affixes; }

    public string GetTooltipText()
    {
        string tooltip = $"<color=#{ColorUtility.ToHtmlStringRGB(rarityColor)}>{equipmentName}</color>\n";
        tooltip += $"{equipmentType} (Level {requiredLevel})\n\n";
        bool wroteAny = false;
        // Base stats first
        if (baseStats != null && baseStats.Count > 0)
        {
            for (int i = 0; i < baseStats.Count; i++)
            {
                var stat = baseStats[i];
                if (!System.Enum.IsDefined(typeof(StatType), (int)stat.statType)) continue;
                tooltip += StatTypeInfo.ToDisplayString(stat) + "\n";
                wroteAny = true;
            }
        }
        // Separator if both base and random exist
        bool hasRandom = randomAffixes != null && randomAffixes.Count > 0;
        if (wroteAny && hasRandom)
        {
            tooltip += "<color=#888888>────────────</color>\n";
        }
        // Random affixes next
        if (hasRandom)
        {
            for (int i = 0; i < randomAffixes.Count; i++)
            {
                var stat = randomAffixes[i];
                if (!System.Enum.IsDefined(typeof(StatType), (int)stat.statType)) continue;
                tooltip += StatTypeInfo.ToDisplayString(stat) + "\n";
            }
        }
        if (!string.IsNullOrEmpty(description)) tooltip += "\n" + description;
        return tooltip;
    }
}
