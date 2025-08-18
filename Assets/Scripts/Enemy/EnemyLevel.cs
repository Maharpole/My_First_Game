using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class EnemyLevel : MonoBehaviour
{
    [Header("Leveling")] public int level = 1;
    [Min(1)] public int baseMaxHealth = 10;
    [Tooltip("Health gained per level above 1")] public int healthPerLevel = 5;
    [Tooltip("Multiplier per level (applied after flat)")] public float healthLevelMultiplier = 1.0f;

    [Header("Experience Reward")] public int baseXP = 5;
    [Tooltip("XP gained per level above 1")] public int xpPerLevel = 2;

    private EnemyHealth _health;

    void Awake()
    {
        _health = GetComponent<EnemyHealth>();
    }

    void Start()
    {
        ApplyLevelToHealth();
    }

    public void ApplyLevelToHealth()
    {
        int clampedLevel = Mathf.Max(1, level);
        int flat = baseMaxHealth + (clampedLevel - 1) * healthPerLevel;
        float scaled = flat * Mathf.Max(0.1f, Mathf.Pow(healthLevelMultiplier, clampedLevel - 1));
        _health.maxHealth = Mathf.Max(1, Mathf.RoundToInt(scaled));
        _health.currentHealth = _health.maxHealth;
    }

    public int GetXPReward()
    {
        int clampedLevel = Mathf.Max(1, level);
        return Mathf.Max(0, baseXP + (clampedLevel - 1) * xpPerLevel);
    }
}





