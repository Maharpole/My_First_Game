using UnityEngine;

[System.Serializable]
public enum EquipmentType
{
    None,
    Helmet,
    Amulet,
    Gloves,
    RingLeft,
    RingRight,
    Boots,
    Belt
}

[System.Serializable]
public enum EquipmentRarity
{
    Normal,     // White items
    Magic,      // Blue items (1-2 modifiers)
    Rare,       // Yellow items (3-6 modifiers)
    Unique      // Orange items (fixed modifiers)
}

[System.Serializable]
public enum StatType
{
    // Core Attributes
    Strength,
    Dexterity,
    Intelligence,
    Vitality,
    
    // Combat Stats
    Damage,
    AttackSpeed,
    CriticalChance,
    CriticalMultiplier,
    
    // Defensive Stats
    Health,
    HealthRegeneration,
    Armor,
    Resistance,
    
    // Utility Stats
    MovementSpeed,
    PickupRadius,
    ExperienceGain
}

[System.Serializable]
public class StatModifier
{
    public StatType statType;
    public float value;
    public bool isPercentage; // true for %, false for flat values
    
    public override string ToString()
    {
        string prefix = value > 0 ? "+" : "";
        string suffix = isPercentage ? "%" : "";
        return $"{prefix}{value}{suffix} {statType}";
    }
}
