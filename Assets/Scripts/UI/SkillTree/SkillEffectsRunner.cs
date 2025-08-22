using UnityEngine;
using System.Collections.Generic;

// Single runner that listens for any node unlock and applies data-driven effects to the Player.
public class SkillEffectsRunner : MonoBehaviour
{
    PlayerSkillHooks _hooks;
    [Header("Nodes to scan on start (optional if using Resources)")]
    public List<SkillNodeData> allNodes = new List<SkillNodeData>();
    [Tooltip("If true, also scan Resources for SkillNodeData on start")] public bool scanResources = true;

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
        // On load, apply effects for already-unlocked nodes
        if (allNodes != null)
        {
            for (int i = 0; i < allNodes.Count; i++)
            {
                var n = allNodes[i];
                if (n != null && SkillTreeState.IsUnlocked(n)) Apply(n);
            }
        }
        if (scanResources)
        {
            var nodes = Resources.LoadAll<SkillNodeData>("");
            if (nodes != null)
            {
                for (int i = 0; i < nodes.Length; i++)
                {
                    var n = nodes[i];
                    if (n != null && SkillTreeState.IsUnlocked(n)) Apply(n);
                }
            }
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
        var list = node.effects;
        if (list == null || list.Count == 0) return; // nothing to do
        for (int i = 0; i < list.Count; i++)
        {
            var e = list[i];
            switch (e.type)
            {
                case SkillNodeData.EffectType.EnableVelocityScale:
                    _hooks.EnableVelocityScale();
                    break;
                case SkillNodeData.EffectType.ReflectFlat:
                    _hooks.AddReflectFlat(Mathf.RoundToInt(e.value));
                    break;
                case SkillNodeData.EffectType.ReflectPercent:
                    _hooks.AddReflectPercent(e.value);
                    break;
                case SkillNodeData.EffectType.MaxHealthFlat:
                    _hooks.AddMaxHealthFlat(Mathf.RoundToInt(e.value));
                    break;
                case SkillNodeData.EffectType.RegenPerSecond:
                    _hooks.AddRegenPerSecond(Mathf.RoundToInt(e.value));
                    break;
                case SkillNodeData.EffectType.EnableMasochism:
                    _hooks.EnableMasochism();
                    break;
            }
        }
        _hooks.Recompute();
        Debug.Log($"[SkillEffectsRunner] Applied {list.Count} data effect(s) for node '{node.id}'.");
    }
}


