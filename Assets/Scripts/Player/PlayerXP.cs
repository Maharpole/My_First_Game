using UnityEngine;
using UnityEngine.Events;

public class PlayerXP : MonoBehaviour
{
    [Header("XP / Level")] public int level = 1;
    public int currentXP = 0;
    public int xpToNextLevel = 20;
    [Tooltip("Additional XP required per level (linear)")] public int xpPerLevel = 10;

    [Header("Events")] public UnityEvent<int> onLevelChanged; // emits new level
    public UnityEvent<int, int> onXPChanged; // emits currentXP, xpToNextLevel

    void Awake()
    {
        if (level < 1) level = 1;
        if (xpToNextLevel < 1) xpToNextLevel = 20;
    }

    public void AddXP(int amount)
    {
        if (amount <= 0) return;
        currentXP += amount;
        while (currentXP >= xpToNextLevel)
        {
            currentXP -= xpToNextLevel;
            level++;
            onLevelChanged?.Invoke(level);
            xpToNextLevel += xpPerLevel;
        }
        onXPChanged?.Invoke(currentXP, xpToNextLevel);
    }
}





