using UnityEngine;

public class EquipmentDropper : MonoBehaviour
{
    [Header("Drop Settings (Legacy)")]
    [Tooltip("Legacy direct list (used if no loot pools provided)")]
    public EquipmentData[] possibleBases;
    [Header("Loot Pools")]
    [Tooltip("Global pool any enemy can use")] public LootPool generalPool;
    [Tooltip("Per-enemy pool to add or replace the general pool")] public LootPool enemySpecificPool;
    [Tooltip("If true, use only the enemySpecificPool; if false, combine general + specific")] public bool replaceGeneralWithSpecific = false;
    public AffixDatabase affixDatabase;
    [Range(0f, 1f)] public float dropChance = 0.35f;
    public int minItemLevel = 1;
    public int maxItemLevel = 20;

    [Header("Spawn Settings")]
    public GameObject pickupPrefab; // should contain RuntimeEquipmentItem
    public float dropSpread = 1.5f;

    [Header("Roll Settings")]
    public int minPrefixes = 0;
    public int maxPrefixes = 3;
    public int minSuffixes = 0;
    public int maxSuffixes = 3;

    void Start()
    {
        var health = GetComponent<EnemyHealth>();
        if (health != null)
        {
            health.onDeath.AddListener(Drop);
        }
    }

    void OnDestroy()
    {
        var health = GetComponent<EnemyHealth>();
        if (health != null)
        {
            health.onDeath.RemoveListener(Drop);
        }
    }

    public void Drop()
    {
        // Early out before any heavy work if no drop occurs
        if (Random.value > dropChance) return;

        // Resolve base pool
        EquipmentData baseItem = null;
        if (generalPool != null || enemySpecificPool != null)
        {
            if (replaceGeneralWithSpecific)
            {
                baseItem = enemySpecificPool != null ? enemySpecificPool.Pick() : null;
            }
            else
            {
                // Combine by picking between pools proportional to their total weights
                var candidates = new System.Collections.Generic.List<(LootPool pool, int total)>();
                int totalWeight = 0;
                if (generalPool != null)
                {
                    int w = 0; foreach (var e in generalPool.entries) if (e != null && e.baseItem != null) w += Mathf.Max(0, e.weight);
                    if (w > 0) { candidates.Add((generalPool, w)); totalWeight += w; }
                }
                if (enemySpecificPool != null)
                {
                    int w = 0; foreach (var e in enemySpecificPool.entries) if (e != null && e.baseItem != null) w += Mathf.Max(0, e.weight);
                    if (w > 0) { candidates.Add((enemySpecificPool, w)); totalWeight += w; }
                }
                if (totalWeight > 0 && candidates.Count > 0)
                {
                    int r = Random.Range(0, totalWeight);
                    int cum = 0;
                    foreach (var c in candidates)
                    {
                        cum += c.total;
                        if (r < cum) { baseItem = c.pool.Pick(); break; }
                    }
                }
            }
        }

        if (baseItem == null)
        {
            if (possibleBases == null || possibleBases.Length == 0) return;
            baseItem = possibleBases[Random.Range(0, possibleBases.Length)];
        }
        if (pickupPrefab == null || affixDatabase == null) return;

        int ilvl = Random.Range(minItemLevel, maxItemLevel + 1);

        var settings = new ItemGenerator.RollSettings
        {
            minPrefixes = minPrefixes,
            maxPrefixes = maxPrefixes,
            minSuffixes = minSuffixes,
            maxSuffixes = maxSuffixes
        };

        var generated = ItemGenerator.Generate(baseItem, ilvl, affixDatabase, settings);

        Vector3 spawnPos = transform.position + new Vector3(Random.Range(-dropSpread, dropSpread), 0.5f, Random.Range(-dropSpread, dropSpread));
        var pickup = Instantiate(pickupPrefab, spawnPos, Quaternion.identity);
        var runtime = pickup.GetComponent<RuntimeEquipmentItem>();
        if (runtime != null)
        {
            runtime.generated = generated;
        }
        Debug.Log($"[EquipmentDropper] Dropped {generated.baseEquipment?.equipmentName} (ilvl {ilvl}) with {generated.prefixes.Count} prefixes / {generated.suffixes.Count} suffixes");
    }
}
