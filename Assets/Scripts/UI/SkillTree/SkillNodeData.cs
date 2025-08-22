using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "SkillTree/Skill Node")]
public class SkillNodeData : ScriptableObject
{
    [Tooltip("Unique id for persistence")] public string id;
    [TextArea] public string description;
    [Tooltip("Parents that must be unlocked before this node (references)")] public List<SkillNodeData> parents = new List<SkillNodeData>();
    [Tooltip("Parent ids (for data-driven graph)")] public List<string> parentIds = new List<string>();

    [Header("Effects on Unlock (LEGACY - safe to leave empty)")] public UnityEvent OnApply;

    [System.Serializable]
    public enum NodeType { Minor, Major }
    [Tooltip("Minor nodes tweak stats; Major nodes can change gameplay")] public NodeType nodeType = NodeType.Minor;

    [System.Serializable]
    public enum StatType
    {
        MaxHealth,
        ReflectFlat,
        ReflectPercent,
        RegenPerSecond
    }

    [System.Serializable]
    public enum ModifierOp { Add, Multiply }

    [System.Serializable]
    public struct StatModifier
    {
        public StatType stat;
        public ModifierOp op;
        public float value;
    }

    [Header("Stat Modifiers (data-driven)")]
    [Tooltip("Numeric stat adjustments applied on unlock")]
    public List<StatModifier> statModifiers = new List<StatModifier>();
    [Min(1)] public int cost = 1;
    public bool allowRefund = false;
}


