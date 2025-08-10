using UnityEngine;
using System.Collections.Generic;

public class RuntimeEquipmentItem : MonoBehaviour
{
    public GeneratedItem generated;

    // Convenience: aggregate all modifiers
    public List<StatModifier> GetAllModifiers()
    {
        return generated != null ? generated.GetAllStatModifiers() : new List<StatModifier>();
    }

    public string GetDisplayName()
    {
        if (generated == null || generated.baseEquipment == null) return "Unknown Item";
        return generated.baseEquipment.equipmentName; // could add prefix/suffix naming later
    }
}
