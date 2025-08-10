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

    [Header("Base Statistics")]
    public List<StatModifier> baseStats = new List<StatModifier>();

    [Header("Affixes (Generated at Runtime)")]
    [SerializeField] private List<StatModifier> randomAffixes = new List<StatModifier>();

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
        // Regular non-hand slots must match exactly
        if (slot != EquipmentType.MainHand && slot != EquipmentType.OffHand)
        {
            return equipmentType == slot;
        }

        // Hand logic
        if (!isWeapon && equipmentType != EquipmentType.OffHand)
        {
            // Not a weapon and not explicitly an offhand type
            return false;
        }

        // Two-hand weapons can go to MainHand only and will occupy both
        if (isWeapon && (handUsage == HandUsage.TwoHand || occupiesBothHands))
        {
            return slot == EquipmentType.MainHand;
        }

        // One-hand weapons can go in MainHand; offhands go in OffHand
        if (slot == EquipmentType.MainHand)
        {
            return isWeapon && handUsage == HandUsage.OneHand;
        }
        if (slot == EquipmentType.OffHand)
        {
            return !isWeapon || equipmentType == EquipmentType.OffHand;
        }

        return false;
    }

    public void SetRandomAffixes(List<StatModifier> affixes)
    {
        randomAffixes = affixes;
    }

    public string GetTooltipText()
    {
        string tooltip = $"<color=#{ColorUtility.ToHtmlStringRGB(rarityColor)}>{equipmentName}</color>\n";
        tooltip += $"{equipmentType} (Level {requiredLevel})\n";
        tooltip += "\n";
        foreach (var stat in AllStats) tooltip += stat.ToString() + "\n";
        if (!string.IsNullOrEmpty(description)) tooltip += "\n" + description;
        return tooltip;
    }
}
