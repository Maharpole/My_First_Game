using UnityEngine;

[CreateAssetMenu(fileName = "MobModifier", menuName = "POE Game/Mobs/Mob Modifier")]
public class MobModifier : ScriptableObject
{
    public string modifierName;
    public string description;
    public int weight = 100;

    [Header("Stat Multipliers (applied to each enemy)")]
    public float moveSpeedMultiplier = 1f;
    public float attackSpeedMultiplier = 1f;
    public float damageMultiplier = 1f;
    public float healthMultiplier = 1f;
    public int extraProjectiles = 0;

    [Header("FX (optional)")]
    public GameObject auraPrefab; // e.g., electrified aura

    public void ApplyToEnemy(EnemyStats stats)
    {
        if (stats == null) return;
        stats.moveSpeed *= moveSpeedMultiplier;
        stats.attackSpeed *= attackSpeedMultiplier;
        stats.damage *= damageMultiplier;
        stats.maxHealth = Mathf.CeilToInt(stats.maxHealth * healthMultiplier);
        stats.currentHealth = Mathf.Min(stats.currentHealth, stats.maxHealth);
        stats.extraProjectiles += extraProjectiles;
    }
}
