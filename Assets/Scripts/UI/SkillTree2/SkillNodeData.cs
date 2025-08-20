using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "SkillTree/Skill Node")]
public class SkillNodeData : ScriptableObject
{
    [Tooltip("Unique id for persistence")] public string id;
    [TextArea] public string description;
    [Tooltip("Parents that must be unlocked before this node")] public List<SkillNodeData> parents = new List<SkillNodeData>();

    [Header("Effects on Unlock")] public UnityEvent OnApply;
    [Min(1)] public int cost = 1;
    public bool allowRefund = false;
}


