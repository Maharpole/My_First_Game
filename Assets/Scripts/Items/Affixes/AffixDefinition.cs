using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Affix", menuName = "POE Game/Affixes/Affix Definition")]
public class AffixDefinition : ScriptableObject
{
    [Header("Identity")]
    public string affixId;             // unique id (e.g., mod_cold_resist)
    public string displayName;         // e.g., "of the Bear" or "Heavy"
    public string modGroup;            // prevent duplicate groups (e.g., "Life")
    public bool isPrefix;              // true = prefix, false = suffix

    [Header("Applicability")]
    public List<EquipmentType> allowedSlots = new List<EquipmentType>();

    [Header("Stat Output")]
    public StatType statType;          // single-stat for simplicity
    public bool isPercentage;          // percent or flat

    [Header("Tiers")] 
    public List<AffixTier> tiers = new List<AffixTier>();

    [Header("Weights")] 
    public int weight = 100;           // selection weight for this affix overall
}

[Serializable]
public class AffixTier
{
    public string tierName = "T1";     // e.g., T1, T2, ...
    public int minItemLevel = 1;       // minimum ilvl to roll this tier
    public float minValue = 1f;        // numeric range
    public float maxValue = 5f;
    public int weight = 100;           // selection weight within this affix
}
