using UnityEngine;
using UnityEngine.Events;

public class PlayerXP : MonoBehaviour
{
    [Header("XP / Level")] public int level = 1;
    public int currentXP = 0;
    public int xpToNextLevel = 20;
    [Tooltip("Additional XP required per level (linear)")] public int xpPerLevel = 10;

    [Header("Skill Points")] public int unspentSkillPoints = 0;
    [Tooltip("Play when the player levels up")] public ParticleSystem levelUpVFX;
    [Tooltip("Sound played on level up")] public AudioClip levelUpSFX;
    [Range(0f,1f)] public float levelUpSFXVolume = 1f;

    [Header("Events")] public UnityEvent<int> onLevelChanged; // emits new level
    public UnityEvent<int, int> onXPChanged; // emits currentXP, xpToNextLevel

    void Awake()
    {
        if (level < 1) level = 1;
        if (xpToNextLevel < 1) xpToNextLevel = 20;
        // Load persisted skill points if any
        unspentSkillPoints = PlayerProfile.UnspentSkillPoints;
    }

    public void AddXP(int amount)
    {
        if (amount <= 0) return;
        currentXP += amount;
        while (currentXP >= xpToNextLevel)
        {
            currentXP -= xpToNextLevel;
            level++;
            unspentSkillPoints++;
            PlayerProfile.UnspentSkillPoints = unspentSkillPoints;
            onLevelChanged?.Invoke(level);
            PlayLevelUpFeedback();
            xpToNextLevel += xpPerLevel;
        }
        onXPChanged?.Invoke(currentXP, xpToNextLevel);
    }

    private void PlayLevelUpFeedback()
    {
        if (levelUpVFX != null)
        {
            levelUpVFX.Clear(true);
            levelUpVFX.Play(true);
        }
        if (levelUpSFX != null)
        {
            var src = GetComponent<AudioSource>();
            if (src == null) src = gameObject.AddComponent<AudioSource>();
            src.PlayOneShot(levelUpSFX, levelUpSFXVolume);
        }
    }
}





