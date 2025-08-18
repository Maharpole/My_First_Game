using UnityEngine;

public static class CombatDebug
{
    // Toggle this at runtime (via inspector script, console, or code) to enable/disable damage logs
    public static bool EnableDamageLogs = false;

    public static void LogDamage(GameObject target, int amount, int currentHp, int maxHp)
    {
        if (!EnableDamageLogs || target == null) return;
        Debug.Log($"[Damage] {target.name} took {amount} damage -> {currentHp}/{maxHp}");
    }
}





