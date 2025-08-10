using UnityEngine;
using System.Collections.Generic;

public class MobDirector : MonoBehaviour
{
    public MapConfig mapConfig;
    public SpawnTable spawnTable;
    public GameObject packSpawnerPrefab; // empty with PackSpawner component

    [Header("NavMesh (optional)")]
    public bool projectToGround = true;
    public LayerMask groundMask;

    public void Generate()
    {
        if (mapConfig == null || spawnTable == null || packSpawnerPrefab == null)
        {
            Debug.LogError("MobDirector: missing references, cannot generate mobs. Please check MapConfig, SpawnTable, and PackSpawnerPrefab.");
            return;
        }

        Random.InitState(mapConfig.seed);

        int packCount = Random.Range(mapConfig.minPacks, mapConfig.maxPacks + 1);
        var points = SamplePoints(mapConfig.worldBounds, packCount, mapConfig.minPackSpacing);

        foreach (var p in points)
        {
            // Roll mob type and modifiers
            var mobType = PickMobType();
            var modifiers = RollModifiers();

            Vector3 pos = p;
            if (projectToGround)
            {
                if (Physics.Raycast(new Vector3(p.x, 100f, p.z), Vector3.down, out var hit, 500f, groundMask))
                {
                    pos = hit.point;
                }
            }

            var go = Instantiate(packSpawnerPrefab, pos, Quaternion.identity);
            var spawner = go.GetComponent<PackSpawner>();
            spawner.Initialize(mobType, modifiers);
            spawner.Spawn();
        }
    }

    List<Vector3> SamplePoints(Bounds b, int targetCount, float minSpacing)
    {
        var list = new List<Vector3>();
        int safety = targetCount * 20;
        while (list.Count < targetCount && safety-- > 0)
        {
            float x = Random.Range(b.min.x, b.max.x);
            float z = Random.Range(b.min.z, b.max.z);
            var p = new Vector3(x, 0f, z);
            bool ok = true;
            foreach (var q in list)
            {
                if ((q - p).sqrMagnitude < minSpacing * minSpacing) { ok = false; break; }
            }
            if (ok) list.Add(p);
        }
        return list;
    }

    MobType PickMobType()
    {
        var entries = spawnTable.mobs;
        int total = 0;
        foreach (var e in entries) total += Mathf.Max(0, e != null ? e.weight : 0);
        if (total <= 0) return null;
        int r = Random.Range(0, total);
        int cum = 0;
        foreach (var e in entries)
        {
            int w = Mathf.Max(0, e != null ? e.weight : 0);
            cum += w;
            if (r < cum) return e.mobType;
        }
        return entries[entries.Length - 1].mobType;
    }

    MobModifier[] RollModifiers()
    {
        int count = Random.Range(spawnTable.minModifiers, spawnTable.maxModifiers + 1);
        var list = new List<MobModifier>(count);
        var entries = spawnTable.modifiers;
        if (entries == null || entries.Length == 0 || count <= 0) return list.ToArray();

        // Simple non-unique selection allowing duplicates avoided by removing picked entries
        var pool = new List<SpawnTable.ModifierEntry>(entries);
        for (int i = 0; i < count && pool.Count > 0; i++)
        {
            int total = 0; foreach (var e in pool) total += Mathf.Max(0, e != null ? e.weight : 0);
            if (total <= 0) break;
            int r = Random.Range(0, total);
            int cum = 0; int idx = -1;
            for (int k = 0; k < pool.Count; k++)
            {
                int w = Mathf.Max(0, pool[k] != null ? pool[k].weight : 0);
                cum += w;
                if (r < cum) { idx = k; break; }
            }
            if (idx >= 0)
            {
                list.Add(pool[idx].modifier);
                pool.RemoveAt(idx);
            }
        }
        return list.ToArray();
    }
}
