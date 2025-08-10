using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "AffixDatabase", menuName = "POE Game/Affixes/Affix Database")]
public class AffixDatabase : ScriptableObject
{
    public List<AffixDefinition> affixes = new List<AffixDefinition>();

    public List<AffixDefinition> GetAffixesForSlot(EquipmentType slot, bool wantPrefix)
    {
        var list = new List<AffixDefinition>();
        foreach (var a in affixes)
        {
            if (a == null) continue;
            if (a.isPrefix != wantPrefix) continue;
            if (a.allowedSlots != null && a.allowedSlots.Count > 0 && !a.allowedSlots.Contains(slot)) continue;
            list.Add(a);
        }
        return list;
    }
}
