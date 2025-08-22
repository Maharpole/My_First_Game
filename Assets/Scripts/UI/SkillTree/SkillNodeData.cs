using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "SkillTree/Skill Node")]
public class SkillNodeData : ScriptableObject
{
    [Tooltip("Unique id for persistence")] public string id;
    [TextArea] public string description;
    [Tooltip("Parents that must be unlocked before this node")] public List<SkillNodeData> parents = new List<SkillNodeData>();

    [Header("Effects on Unlock (LEGACY - safe to leave empty)")] public UnityEvent OnApply;

    [System.Serializable]
    public enum EffectType
    {
        EnableVelocityScale,
        ReflectFlat,
        MaxHealthFlat,
        RegenPerSecond,
        ReflectPercent,
        EnableMasochism
    }

    [System.Serializable]
    public struct Effect
    {
        public EffectType type;
        public float value;
    }

    [Header("Data Effects (no scene refs)")]
    [Tooltip("Optional data-driven effects; applied by SkillEffectsRunner on the Player when this node unlocks.")]
    public List<Effect> effects = new List<Effect>();
    [Min(1)] public int cost = 1;
    public bool allowRefund = false;
}


