using UnityEngine;
using System.Collections.Generic;

public static class ItemGenerator
{
    public const int MaxItemLevel = 100;

    public struct RollSettings
    {
        public int minPrefixes;
        public int maxPrefixes;
        public int minSuffixes;
        public int maxSuffixes;

        public int[] prefixCountWeights; // optional
        public int[] suffixCountWeights; // optional

        // Rarity chances (0..1 probabilities). Will be normalized if needed.
        public float chanceCommon; // white
        public float chanceMagic;  // blue
        public float chanceRare;   // yellow

        // Tier bias (>1 prefers lower tiers, <1 prefers higher tiers). 1 = neutral
        public float tierBias;
    }

    public static GeneratedItem Generate(EquipmentData baseItem, int itemLevel, AffixDatabase db, RollSettings settings)
    {
        // Clamp item level to [1, MaxItemLevel]
        itemLevel = Mathf.Clamp(itemLevel, 1, MaxItemLevel);

        var result = new GeneratedItem
        {
            baseEquipment = baseItem,
            itemLevel = itemLevel,
            prefixes = new List<GeneratedAffix>(),
            suffixes = new List<GeneratedAffix>(),
            rarity = ItemRarity.Common
        };

        if (db == null || baseItem == null)
            return result;

        // Determine rarity
        result.rarity = RollRarity(settings);

        // Determine affix counts based on rarity
        int desiredPrefixes = 0;
        int desiredSuffixes = 0;

        switch (result.rarity)
        {
            case ItemRarity.Common:
                desiredPrefixes = 0;
                desiredSuffixes = 0;
                break;
            case ItemRarity.Magic:
                // at least one affix: either 1P, 1S, or both
                bool rollBoth = Random.value < 0.5f;
                if (rollBoth) { desiredPrefixes = 1; desiredSuffixes = 1; }
                else if (Random.value < 0.5f) { desiredPrefixes = 1; desiredSuffixes = 0; }
                else { desiredPrefixes = 0; desiredSuffixes = 1; }
                break;
            case ItemRarity.Rare:
                // 0-3 each; use the provided weight arrays if present
                desiredPrefixes = PickCount(settings.minPrefixes, settings.maxPrefixes, settings.prefixCountWeights);
                desiredSuffixes = PickCount(settings.minSuffixes, settings.maxSuffixes, settings.suffixCountWeights);
                break;
        }

        // Roll prefixes
        RollAffixes(db.GetAffixesForSlot(baseItem.equipmentType, true), desiredPrefixes, itemLevel, result.prefixes, settings.tierBias);
        // Roll suffixes
        RollAffixes(db.GetAffixesForSlot(baseItem.equipmentType, false), desiredSuffixes, itemLevel, result.suffixes, settings.tierBias);

        return result;
    }

    static ItemRarity RollRarity(RollSettings s)
    {
        float c = Mathf.Max(0f, s.chanceCommon);
        float m = Mathf.Max(0f, s.chanceMagic);
        float r = Mathf.Max(0f, s.chanceRare);
        float total = c + m + r;
        if (total <= 0f) { c = 0.6f; m = 0.3f; r = 0.1f; total = 1f; }
        float roll = Random.value * total;
        if (roll < c) return ItemRarity.Common;
        roll -= c;
        if (roll < m) return ItemRarity.Magic;
        return ItemRarity.Rare;
    }

    static int PickCount(int min, int max, int[] weights)
    {
        if (min > max) { var tmp = min; min = max; max = tmp; }
        if (weights == null || weights.Length == 0)
        {
            return Random.Range(min, max + 1);
        }

        int rangeSize = (max - min) + 1;
        var compact = new List<(int count, int weight)>(rangeSize);
        for (int c = min; c <= max; c++)
        {
            int w = (c >= 0 && c < weights.Length) ? Mathf.Max(0, weights[c]) : 0;
            compact.Add((c, w));
        }

        int total = 0; foreach (var t in compact) total += t.weight;
        if (total <= 0) return Random.Range(min, max + 1);

        int r = Random.Range(0, total);
        int cum = 0;
        foreach (var t in compact)
        {
            cum += t.weight;
            if (r < cum) return t.count;
        }
        return max;
    }

    static void RollAffixes(List<AffixDefinition> pool, int count, int itemLevel, List<GeneratedAffix> output, float tierBias)
    {
        if (pool == null || pool.Count == 0 || count <= 0) return;

        var usedGroups = new HashSet<string>();
        int safety = 100;

        while (output.Count < count && safety-- > 0)
        {
            var affix = WeightedPick(pool);
            if (affix == null) break;
            if (!string.IsNullOrEmpty(affix.modGroup) && usedGroups.Contains(affix.modGroup))
            {
                continue; // prevent duplicate mod group
            }

            var tier = WeightedPickTier(affix.tiers, itemLevel, tierBias);
            if (tier == null) continue;

            float roll = Random.Range(tier.minValue, tier.maxValue);

            output.Add(new GeneratedAffix
            {
                affixId = affix.affixId,
                tierName = tier.tierName,
                isPrefix = affix.isPrefix,
                statType = affix.statType,
                isPercentage = affix.isPercentage,
                value = roll,
                modGroup = affix.modGroup
            });

            if (!string.IsNullOrEmpty(affix.modGroup))
            {
                usedGroups.Add(affix.modGroup);
            }
        }
    }

    static AffixDefinition WeightedPick(List<AffixDefinition> list)
    {
        int total = 0;
        foreach (var a in list) total += Mathf.Max(0, a != null ? a.weight : 0);
        if (total <= 0) return null;
        int r = Random.Range(0, total);
        int cum = 0;
        foreach (var a in list)
        {
            int w = Mathf.Max(0, a != null ? a.weight : 0);
            cum += w;
            if (r < cum) return a;
        }
        return list[list.Count - 1];
    }

    static AffixTier WeightedPickTier(List<AffixTier> tiers, int itemLevel, float tierBias = 1f)
    {
        if (tiers == null || tiers.Count == 0) return null;

        // Build weighted list honoring item level and bias
        var weighted = new List<(AffixTier t, float w)>();
        for (int i = 0; i < tiers.Count; i++)
        {
            var t = tiers[i];
            if (t == null) continue;
            if (itemLevel < t.minItemLevel) continue;
            float w = Mathf.Max(0, t.weight);
            if (tierBias > 0f && tierBias != 1f)
            {
                // Bias by index: index 0 = top tier
                float norm = (tiers.Count <= 1) ? 0f : (float)i / (tiers.Count - 1);
                float biasScale = Mathf.Lerp(1f / Mathf.Max(0.01f, tierBias), tierBias, norm);
                w *= biasScale;
            }
            if (w > 0f) weighted.Add((t, w));
        }

        if (weighted.Count == 0) return null;
        float total = 0f; foreach (var e in weighted) total += e.w;
        float r = Random.value * total;
        float cum = 0f;
        foreach (var e in weighted)
        {
            cum += e.w;
            if (r < cum) return e.t;
        }
        return weighted[weighted.Count - 1].t;
    }
}
