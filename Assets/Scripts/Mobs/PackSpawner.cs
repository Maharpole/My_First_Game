using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

public class PackSpawner : MonoBehaviour
{
    public MobType mobType;
    public MobModifier[] appliedModifiers;
    public float spawnRadius = 3f;

    [Header("Grounding")]
    public bool projectEachToGround = true;
    public LayerMask groundMask;
    public float rayStartHeight = 5f;
    public float heightOffset = 0.05f; // nudge up to avoid clipping

    private AggroGroup aggroGroup;

    public void Initialize(MobType type, MobModifier[] modifiers)
    {
        mobType = type;
        appliedModifiers = modifiers;
    }

    public void Spawn()
    {
        if (mobType == null || mobType.composition == null || mobType.composition.Length == 0)
        {
            Debug.LogWarning("PackSpawner: Missing mobType or composition");
            return;
        }

        aggroGroup = gameObject.AddComponent<AggroGroup>();

        int count = Random.Range(mobType.minCount, mobType.maxCount + 1);
        for (int i = 0; i < count; i++)
        {
            var entry = WeightedPick(mobType.composition);
            if (entry == null || entry.enemyType == null || entry.enemyType.prefab == null) continue;

            Vector2 circle = Random.insideUnitCircle * spawnRadius;
            Vector3 pos = transform.position + new Vector3(circle.x, 0f, circle.y);

            // Project to ground or NavMesh per unit, to avoid under-ground spawns after ground scaling.
            if (projectEachToGround)
            {
                // Try ground raycast first
                Vector3 rayStart = pos + Vector3.up * rayStartHeight;
                if (Physics.Raycast(rayStart, Vector3.down, out var hit, rayStartHeight * 2f, groundMask))
                {
                    pos = hit.point + Vector3.up * heightOffset;
                }
                else
                {
                    // Fallback to NavMesh sample
                    if (NavMesh.SamplePosition(pos, out var navHit, 3f, NavMesh.AllAreas))
                    {
                        pos = navHit.position + Vector3.up * heightOffset;
                    }
                }
            }

            var enemy = Object.Instantiate(entry.enemyType.prefab, pos, Quaternion.identity);

            var stats = enemy.GetComponent<EnemyHealth>();

            // Apply modifiers
            if (appliedModifiers != null)
            {
                foreach (var mod in appliedModifiers)
                {
                    mod?.ApplyToEnemy(stats);
                }
            }

            if (aggroGroup != null)
            {
                aggroGroup.Register(enemy);
            }
        }
    }

    private MobType.Entry WeightedPick(MobType.Entry[] list)
    {
        int total = 0;
        foreach (var e in list) total += Mathf.Max(0, e != null ? e.weight : 0);
        if (total <= 0) return null;
        int r = Random.Range(0, total);
        int cum = 0;
        foreach (var e in list)
        {
            int w = Mathf.Max(0, e != null ? e.weight : 0);
            cum += w;
            if (r < cum) return e;
        }
        return list[list.Length - 1];
    }
}
