using System.Reflection;
using UnityEngine;

public class PlayerSkillHooks : MonoBehaviour
{
    [Header("References")] public Player player;
    public VelocityScaleBySpeed velocityScaler;

    [Header("Skill Totals (runtime)")]
    [SerializeField] int skillReflectFlat;
    [SerializeField] int skillMaxHealthFlat;
    [SerializeField] int skillRegenPerSecond;

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
    }

    void OnDisable()
    {
        if (player != null) player.onEquipmentChanged.RemoveListener(OnEquipmentChanged_ReapplyHealthBonus);
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
        if (skillRegenPerSecond <= 0 || player == null) return;
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
            int next = Mathf.Min(player.maxHealth, cur + Mathf.Max(1, skillRegenPerSecond));
            if (_fiCurrentHealth != null)
            {
                _fiCurrentHealth.SetValue(player, next);
            }
            player.onHealthChanged?.Invoke(next);
        }
    }

    // Optional helper for future integration: expose current skill reflect
    public int GetSkillReflectFlat() => skillReflectFlat;
}


