using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LootPool", menuName = "POE Game/Loot/Loot Pool")]
public class LootPool : ScriptableObject
{
    [System.Serializable]
    public class LootEntry
    {
        public EquipmentData baseItem;
        [Tooltip("Relative selection weight for this base item")] public int weight = 100;
    }

    public List<LootEntry> entries = new List<LootEntry>();

    public EquipmentData Pick()
    {
        if (entries == null || entries.Count == 0) return null;
        int total = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (e == null || e.baseItem == null) continue;
            total += Mathf.Max(0, e.weight);
        }
        if (total <= 0) return null;
        int r = Random.Range(0, total);
        int cum = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (e == null || e.baseItem == null) continue;
            cum += Mathf.Max(0, e.weight);
            if (r < cum) return e.baseItem;
        }
        return entries[entries.Count - 1].baseItem;
    }
}



