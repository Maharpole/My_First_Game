using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GeneratedItem
{
    public EquipmentData baseEquipment;         // reference to base item (for icon, model, base stats)
    public int itemLevel;
    public ItemRarity rarity;
    public List<GeneratedAffix> prefixes = new List<GeneratedAffix>();
    public List<GeneratedAffix> suffixes = new List<GeneratedAffix>();

    public List<StatModifier> GetAllStatModifiers()
    {
        var list = new List<StatModifier>();
        // base stats
        if (baseEquipment != null)
        {
            list.AddRange(baseEquipment.baseStats);
        }
        // prefix/suffix stats
        foreach (var p in prefixes)
        {
            list.Add(new StatModifier { statType = p.statType, value = p.value, isPercentage = p.isPercentage });
        }
        foreach (var s in suffixes)
        {
            list.Add(new StatModifier { statType = s.statType, value = s.value, isPercentage = s.isPercentage });
        }
        return list;
    }
}
