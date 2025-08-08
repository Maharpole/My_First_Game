using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Equipment", menuName = "POE Game/Equipment/Equipment Data")]
public class EquipmentData : ScriptableObject
{
    [Header("Basic Information")]
    public string equipmentName;
    [TextArea(3, 5)]
    public string description;
    public Sprite icon;
    public GameObject modelPrefab; // 3D model for the equipment
    
    [Header("Equipment Properties")]
    public EquipmentType equipmentType;
    public EquipmentRarity rarity = EquipmentRarity.Normal;
    public int itemLevel = 1;
    public int requiredLevel = 1;
    
    [Header("Base Statistics")]
    public List<StatModifier> baseStats = new List<StatModifier>();
    
    [Header("Affixes (Generated at Runtime)")]
    [SerializeField] 
    private List<StatModifier> randomAffixes = new List<StatModifier>(); // These will be generated
    
    [Header("Visual Settings")]
    public Color rarityColor = Color.white;
    
    // Properties
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
        return equipmentType == slot;
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
        
        // Add all stats
        foreach (var stat in AllStats)
        {
            tooltip += stat.ToString() + "\n";
        }
        
        if (!string.IsNullOrEmpty(description))
        {
            tooltip += "\n" + description;
        }
        
        return tooltip;
    }
}
