using UnityEngine;

[System.Serializable]
public enum EquipmentType
{
    None,
    Helmet,
    BodyArmour,
    Amulet,
    Gloves,
    Ring,
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
    // NOTE: Do NOT reorder existing entries.
    // Pin numeric values so serialized data stays stable. Only APPEND new entries with explicit values.
    
    // Core Attributes
    Strength = 0,
    Dexterity = 1,
    Intelligence = 2,
    // 3 was Vitality (deprecated) – reserved to preserve serialized stability
    
    // Combat Stats
    Damage = 4,
    AttackSpeed = 5,
    CriticalChance = 6,
    CriticalMultiplier = 7,
    
    // Defensive Stats
    Health = 8,
    HealthRegeneration = 9,
    Armor = 10,
    Resistance = 11,
    DamageReflectFlat = 12,
    DamageReflectPercent = 13,
    
    // Utility Stats
    [System.Obsolete("Use MovementSpeedFlat or MovementSpeedPercent")] MovementSpeed = 14,
    PickupRadius = 15,
    ExperienceGain = 16,

    // Newly added (append-only): Do not change assigned values
    MaxHealth = 17,              // flat increased maximum health
    MaxHealthPercent = 18,       // % increased maximum health

    // Split ambiguous stats into explicit kinds
    DamageFlat = 19,
    DamagePercent = 20,
    MovementSpeedFlat = 21,
    MovementSpeedPercent = 22
}

[System.Serializable]
public enum StatValueKind
{
    FlatOnly,
    PercentOnly,
    FlatOrPercent
}

public static class StatTypeInfo
{
    public static StatValueKind GetValueKind(StatType type)
    {
        switch (type)
        {
            // Core
            case StatType.Strength:
            case StatType.Dexterity:
            case StatType.Intelligence:
                return StatValueKind.FlatOnly;

            // Combat
            case StatType.Damage:
                return StatValueKind.FlatOrPercent; // legacy
            case StatType.DamageFlat:
                return StatValueKind.FlatOnly;
            case StatType.DamagePercent:
                return StatValueKind.PercentOnly;
            case StatType.AttackSpeed:
                return StatValueKind.PercentOnly;
            case StatType.CriticalChance:
            case StatType.CriticalMultiplier:
                return StatValueKind.PercentOnly;

            // Defensive
            case StatType.Health:
                return StatValueKind.FlatOrPercent; // legacy generic, prefer MaxHealth/MaxHealthPercent
            case StatType.MaxHealth:
                return StatValueKind.FlatOnly;
            case StatType.MaxHealthPercent:
                return StatValueKind.PercentOnly;
            case StatType.HealthRegeneration:
                return StatValueKind.FlatOrPercent;
            case StatType.Armor:
                return StatValueKind.FlatOnly;
            case StatType.Resistance:
                return StatValueKind.PercentOnly;
            case StatType.DamageReflectFlat:
                return StatValueKind.FlatOnly;
            case StatType.DamageReflectPercent:
                return StatValueKind.PercentOnly;

            // Utility
            case StatType.MovementSpeedFlat:
                return StatValueKind.FlatOnly;
            case StatType.MovementSpeedPercent:
                return StatValueKind.PercentOnly;
            case StatType.PickupRadius:
                return StatValueKind.FlatOnly;
            case StatType.ExperienceGain:
                return StatValueKind.PercentOnly;
        }
        return StatValueKind.FlatOrPercent;
    }

    public static string GetDisplayLabel(StatType type)
    {
        switch (type)
        {
            case StatType.AttackSpeed: return "Attack Speed";
            case StatType.CriticalChance: return "Critical Strike Chance";
            case StatType.CriticalMultiplier: return "Critical Strike Multiplier";
            case StatType.Health: return "Maximum Health"; // legacy generic
            case StatType.MaxHealth: return "Maximum Health";
            case StatType.MaxHealthPercent: return "Maximum Health";
            case StatType.DamageFlat: return "Damage";
            case StatType.DamagePercent: return "Damage";
            case StatType.MovementSpeedFlat: return "Movement Speed";
            case StatType.MovementSpeedPercent: return "Movement Speed";
            case StatType.HealthRegeneration: return "Health Regeneration";
            case StatType.DamageReflectFlat: return "Damage Reflected to Attackers";
            case StatType.DamageReflectPercent: return "Damage Reflected to Attackers";
            // Legacy MovementSpeed removed from labels to avoid obsolete warnings
            case StatType.PickupRadius: return "Pickup Radius";
            case StatType.ExperienceGain: return "Experience Gain";
            default:
                return type.ToString();
        }
    }

    public static bool EffectiveIsPercent(StatType type, bool isPercentageFlag)
    {
        var kind = GetValueKind(type);
        if (kind == StatValueKind.PercentOnly) return true;
        if (kind == StatValueKind.FlatOnly) return false;
        return isPercentageFlag;
    }

    public static string ToDisplayString(StatModifier mod)
    {
        bool isPct = EffectiveIsPercent(mod.statType, mod.isPercentage);
        string label = GetDisplayLabel(mod.statType);
        string prefix = mod.value > 0 ? "+" : "";
        string suffix = isPct ? "%" : "";
        if (isPct)
        {
            return $"{prefix}{mod.value}{suffix} increased {label}";
        }
        return $"{prefix}{mod.value}{suffix} {label}";
    }
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
        return StatTypeInfo.ToDisplayString(this);
    }
}
