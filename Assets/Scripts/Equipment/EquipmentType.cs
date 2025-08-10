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
    Belt,
    MainHand,
    OffHand
}

[System.Serializable]
public enum EquipmentRarity
{
    Common,     // White items
    Magic,      // Blue items (1-2 modifiers)
    Rare        // Yellow items (3-6 modifiers)
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

[System.Flags]
public enum OffhandCategory
{
    None = 0,
    Shield = 1 << 0,
    Quiver = 1 << 1,
    Focus = 1 << 2
}

public enum HandUsage
{
    OneHand,
    TwoHand
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
