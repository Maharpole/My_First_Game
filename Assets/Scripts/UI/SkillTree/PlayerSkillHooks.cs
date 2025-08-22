using System.Reflection;
using UnityEngine;

public class PlayerSkillHooks : MonoBehaviour
{
    [Header("References")] public Player player;
    public VelocityScaleBySpeed velocityScaler;

    [Header("Skill Totals (runtime)")]
    [SerializeField] int skillReflectFlat;
    [SerializeField] float skillReflectPercent;
    [SerializeField] int skillMaxHealthFlat;
    [SerializeField] int skillRegenPerSecond;
    
    [Header("Masochism")] public bool masochismEnabled;
    [SerializeField] int masochismStacks;
    [SerializeField] float masochismExpireAt;
    public int masochismMaxStacks = 10;
    public float masochismDuration = 3f;
    public float masochismRegenPerStack = 1f;
    public float masochismReflectPercentPerStack = 2f;

    FieldInfo _fiCurrentHealth;
    FieldInfo _fiBaseMaxHealth;
    int _baseMaxHealthInitial;
    Coroutine _regenRoutine;

    void Awake()
    {
        if (player == null) player = Object.FindFirstObjectByType<Player>();
        if (velocityScaler == null && player != null)
        {
            velocityScaler = player.GetComponent<VelocityScaleBySpeed>();
            if (velocityScaler == null) velocityScaler = player.gameObject.AddComponent<VelocityScaleBySpeed>();
            velocityScaler.enabledBySkill = false;
        }
        if (player != null)
        {
            _fiCurrentHealth = typeof(Player).GetField("currentHealth", BindingFlags.NonPublic | BindingFlags.Instance);
            _fiBaseMaxHealth = typeof(Player).GetField("baseMaxHealth", BindingFlags.NonPublic | BindingFlags.Instance);
            if (_fiBaseMaxHealth != null)
            {
                _baseMaxHealthInitial = (int)_fiBaseMaxHealth.GetValue(player);
            }
        }
    }

    void OnEnable()
    {
        if (player != null) player.onEquipmentChanged.AddListener(OnEquipmentChanged_ReapplyHealthBonus);
        if (player != null) player.onDamaged.AddListener(OnPlayerDamaged);
    }

    void OnDisable()
    {
        if (player != null) player.onEquipmentChanged.RemoveListener(OnEquipmentChanged_ReapplyHealthBonus);
        if (player != null) player.onDamaged.RemoveListener(OnPlayerDamaged);
        StopRegen();
    }

    // === Public API for node OnApply events ===
    public void EnableVelocityScale()
    {
        if (velocityScaler == null) return;
        velocityScaler.enabledBySkill = true;
    }

    public void AddReflectFlat(int amount)
    {
        skillReflectFlat += Mathf.Max(0, amount);
        // Integration note: hook this into your damage reflection pipeline wherever reflect is computed
    }

    public void AddReflectPercent(float percent)
    {
        skillReflectPercent += Mathf.Max(0f, percent);
    }

    public void AddMaxHealthFlat(int amount)
    {
        if (player == null) return;
        int add = Mathf.Max(0, amount);
        skillMaxHealthFlat += add;
        if (_fiBaseMaxHealth != null)
        {
            int before = (int)_fiBaseMaxHealth.GetValue(player);
            int target = Mathf.Max(1, _baseMaxHealthInitial + skillMaxHealthFlat);
            _fiBaseMaxHealth.SetValue(player, target);
            Debug.Log($"[SkillHooks] AddMaxHealthFlat +{add}: baseMaxHealth {before} -> {target}");
            player.RecomputeAndApplyStats();
        }
        else
        {
            int before = player.maxHealth;
            player.maxHealth = Mathf.Max(1, player.maxHealth + add);
            Debug.Log($"[SkillHooks] AddMaxHealthFlat fallback +{add}: maxHealth {before} -> {player.maxHealth}");
        }
        ClampCurrentHealthToMax();
    }

    public void AddRegenPerSecond(int amount)
    {
        int add = Mathf.Max(0, amount);
        skillRegenPerSecond += add;
        RestartRegen();
    }

    public void EnableMasochism()
    {
        masochismEnabled = true;
        masochismStacks = 0;
        masochismExpireAt = 0f;
    }

    public void ApplyStatModifier(SkillNodeDefinition.StatModifier mod)
    {
        switch (mod.stat)
        {
            case SkillNodeDefinition.StatType.MaxHealth:
                if (mod.op == SkillNodeDefinition.ModifierOp.Add)
                    AddMaxHealthFlat(Mathf.RoundToInt(mod.value));
                break;
            case SkillNodeDefinition.StatType.ReflectFlat:
                if (mod.op == SkillNodeDefinition.ModifierOp.Add)
                    AddReflectFlat(Mathf.RoundToInt(mod.value));
                break;
            case SkillNodeDefinition.StatType.ReflectPercent:
                if (mod.op == SkillNodeDefinition.ModifierOp.Add)
                    AddReflectPercent(mod.value);
                break;
            case SkillNodeDefinition.StatType.RegenPerSecond:
                if (mod.op == SkillNodeDefinition.ModifierOp.Add)
                    AddRegenPerSecond(Mathf.RoundToInt(mod.value));
                break;
            default:
                Debug.LogWarning($"[PlayerSkillHooks] Unhandled stat {mod.stat}");
                break;
        }
    }

    public void Recompute()
    {
        // Optional sync point: ensure health is clamped and UI refreshed
        if (player != null)
        {
            Debug.Log("[SkillHooks] Recompute called → RecomputeAndApplyStats");
            player.RecomputeAndApplyStats();
        }
        ClampCurrentHealthToMax();
    }

    // === Internals ===
    void OnEquipmentChanged_ReapplyHealthBonus()
    {
        if (player == null) return;
        if (skillMaxHealthFlat != 0)
        {
            if (_fiBaseMaxHealth != null)
            {
                _fiBaseMaxHealth.SetValue(player, Mathf.Max(1, _baseMaxHealthInitial + skillMaxHealthFlat));
                player.RecomputeAndApplyStats();
            }
            else
            {
                player.maxHealth = Mathf.Max(1, player.maxHealth + skillMaxHealthFlat);
            }
            ClampCurrentHealthToMax();
        }
    }

    void ClampCurrentHealthToMax()
    {
        if (player == null || _fiCurrentHealth == null) return;
        int cur = (int)_fiCurrentHealth.GetValue(player);
        int clamped = Mathf.Clamp(cur, 1, player.maxHealth);
        if (clamped != cur)
        {
            _fiCurrentHealth.SetValue(player, clamped);
            player.onHealthChanged?.Invoke(clamped);
        }
        else
        {
            // Still notify UI if needed
            player.onHealthChanged?.Invoke(clamped);
        }
    }

    void RestartRegen()
    {
        StopRegen();
        if ((skillRegenPerSecond + GetMasochismRegenBonus()) <= 0 || player == null) return;
        _regenRoutine = StartCoroutine(RegenLoop());
    }

    void StopRegen()
    {
        if (_regenRoutine != null)
        {
            StopCoroutine(_regenRoutine);
            _regenRoutine = null;
        }
    }

    System.Collections.IEnumerator RegenLoop()
    {
        var wait = new WaitForSeconds(1f);
        while (true)
        {
            yield return wait;
            if (player == null) continue;
            if (!player.IsAlive) continue;
            int cur = _fiCurrentHealth != null ? (int)_fiCurrentHealth.GetValue(player) : player.CurrentHealth;
            int regen = Mathf.RoundToInt(skillRegenPerSecond + GetMasochismRegenBonus());
            int next = Mathf.Min(player.maxHealth, cur + Mathf.Max(1, regen));
            if (_fiCurrentHealth != null)
            {
                _fiCurrentHealth.SetValue(player, next);
            }
            player.onHealthChanged?.Invoke(next);
        }
    }

    void Update()
    {
        if (!masochismEnabled) return;
        if (masochismStacks > 0 && Time.time >= masochismExpireAt)
        {
            masochismStacks = 0;
            masochismExpireAt = 0f;
            // Recalculate regen pipeline
            RestartRegen();
        }
    }

    void OnPlayerDamaged(int dmg)
    {
        if (!masochismEnabled) return;
        // Gain or refresh stack
        if (masochismStacks < masochismMaxStacks)
        {
            masochismStacks++;
        }
        // Refresh expiry
        masochismExpireAt = Time.time + masochismDuration;
        // Ensure regen reflects new stacks
        RestartRegen();
    }

    float GetMasochismRegenBonus()
    {
        return masochismEnabled ? masochismRegenPerStack * Mathf.Max(0, masochismStacks) : 0f;
    }

    public float GetMasochismReflectPercent()
    {
        return masochismEnabled ? masochismReflectPercentPerStack * Mathf.Max(0, masochismStacks) : 0f;
    }

    // Optional helper for future integration: expose current skill reflect
    public int GetSkillReflectFlat() => skillReflectFlat;
    public float GetSkillReflectPercent() => skillReflectPercent + GetMasochismReflectPercent();
    public int GetMasochismStacks() => masochismStacks;
    public float GetMasochismTimeRemaining() => Mathf.Max(0f, masochismExpireAt - Time.time);
}


