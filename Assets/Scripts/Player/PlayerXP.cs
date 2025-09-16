using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class PlayerXP : MonoBehaviour
{
    [Header("XP / Level")] public int level = 1;
    public int currentXP = 0;
    public int xpToNextLevel = 20;
    [Tooltip("Additional XP required per level (linear) - deprecated when using table")] public int xpPerLevel = 10;

    // XP to gain per level (1..100). Index 0 => level 1, value is XP to reach level 2.
    // Values provided by user table. Level 100 requires 0 XP (cap).
    static readonly int[] XP_TO_GAIN = new int[]
    {
        525, 1235, 2021, 3403, 5002, 7138, 10053, 13804, 18512, 24297,
        31516, 39878, 50352, 62261, 76465, 92806, 112027, 133876, 158538, 187025,
        218895, 255366, 295852, 341805, 392470, 449555, 512121, 583857, 662181, 747411,
        844146, 949053, 1064952, 1192712, 1333241, 1487491, 1656447, 1841143, 2046202, 2265837,
        2508528, 2776124, 3061734, 3379914, 3723676, 4099570, 4504444, 4951099, 5430907, 5957868,
        6528910, 7153414, 7827968, 8555414, 9353933, 10212541, 11142646, 12157041, 13252160, 14441758,
        15731508, 17127265, 18635053, 20271765, 22044909, 23950783, 26019833, 28261412, 30672515, 33287878,
        36118904, 39163425, 42460810, 46024718, 49853964, 54008554, 58473753, 63314495, 68516464, 74132190,
        80182477, 86725730, 93748717, 101352108, 109524907, 118335069, 127813148, 138033822, 149032822, 160890604,
        173648795, 187372170, 202153736, 218041909, 235163399, 253547862, 273358532, 294631836, 317515914, 0
    };

    [Header("Skill Points")] public int unspentSkillPoints = 0;
    [Tooltip("Play when the player levels up")] public ParticleSystem levelUpVFX;
    [Tooltip("Sound played on level up")] public AudioClip levelUpSFX;
    [Range(0f,1f)] public float levelUpSFXVolume = 1f;

    [Header("Events")] public UnityEvent<int> onLevelChanged; // emits new level
    public UnityEvent<int, int> onXPChanged; // emits currentXP, xpToNextLevel

    [Header("UI")] public TMP_Text levelText;
    [Tooltip("Format string for level label; {0}=current level")] public string levelTextFormat = "Lv {0}";

    void Awake()
    {
        if (level < 1) level = 1;
        // Initialize from table if available
        xpToNextLevel = GetXpToNextForLevel(level);
        // Load persisted skill points if any
        unspentSkillPoints = PlayerProfile.UnspentSkillPoints;
        UpdateLevelLabel();
    }

    void OnEnable()
    {
        if (onLevelChanged != null) onLevelChanged.AddListener(HandleLevelChanged);
        UpdateLevelLabel();
    }

    void OnDisable()
    {
        if (onLevelChanged != null) onLevelChanged.RemoveListener(HandleLevelChanged);
    }

    public void AddXP(int amount)
    {
        if (amount <= 0) return;
        currentXP += amount;
        while (currentXP >= xpToNextLevel && xpToNextLevel > 0)
        {
            currentXP -= xpToNextLevel;
            level = Mathf.Min(100, level + 1);
            unspentSkillPoints++;
            PlayerProfile.UnspentSkillPoints = unspentSkillPoints;
            onLevelChanged?.Invoke(level);
            PlayLevelUpFeedback();
            xpToNextLevel = GetXpToNextForLevel(level);
        }
        onXPChanged?.Invoke(currentXP, xpToNextLevel);
    }

    int GetXpToNextForLevel(int lvl)
    {
        int cl = Mathf.Clamp(lvl, 1, XP_TO_GAIN.Length);
        return XP_TO_GAIN[cl - 1];
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

    void HandleLevelChanged(int newLevel)
    {
        UpdateLevelLabel();
    }

    public void UpdateLevelLabel()
    {
        if (levelText != null)
        {
            levelText.text = string.Format(levelTextFormat, level);
        }
    }
}





