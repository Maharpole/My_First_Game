using UnityEngine;

public class EquipmentDropper : MonoBehaviour
{
    [Header("Drop Settings")]
    public EquipmentData[] possibleBases;
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
        if (possibleBases == null || possibleBases.Length == 0) return;
        if (Random.value > dropChance) return;
        if (pickupPrefab == null || affixDatabase == null) return;

        var baseItem = possibleBases[Random.Range(0, possibleBases.Length)];
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
