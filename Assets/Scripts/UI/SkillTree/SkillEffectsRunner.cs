using UnityEngine;
// Single runner that listens for any node unlock and applies data-driven effects to the Player.
public class SkillEffectsRunner : MonoBehaviour
{
    PlayerSkillHooks _hooks;

    void Awake()
    {
        _hooks = FindFirstObjectByType<PlayerSkillHooks>();
    }

    void OnEnable()
    {
        SkillTreeState.OnUnlocked += HandleUnlocked;
    }

    void OnDisable()
    {
        SkillTreeState.OnUnlocked -= HandleUnlocked;
    }

    void Start()
    {
        // Build database and apply effects for already-unlocked nodes
        SkillNodeDatabase.LoadAll();
        foreach (var n in SkillNodeDatabase.All)
        {
            if (n != null && SkillTreeState.IsUnlocked(n)) Apply(n);
        }
    }

    void HandleUnlocked(SkillNodeData node)
    {
        Apply(node);
    }

    void Apply(SkillNodeData node)
    {
        if (node == null) return;
        if (_hooks == null) _hooks = FindFirstObjectByType<PlayerSkillHooks>();
        if (_hooks == null)
        {
            Debug.LogWarning("[SkillEffectsRunner] PlayerSkillHooks not found; cannot apply node effects.");
            return;
        }
        var list = node.statModifiers;
        if (list != null)
        {
            for (int i = 0; i < list.Count; i++)
            {
                _hooks.ApplyStatModifier(list[i]);
            }
        }
        _hooks.Recompute();
        Debug.Log($"[SkillEffectsRunner] Applied {(list!=null?list.Count:0)} modifier(s) for node '{node.id}'.");
    }
}


