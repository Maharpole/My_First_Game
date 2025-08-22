using System.Collections.Generic;
using UnityEngine;

public static class SkillNodeDatabase
{
    static Dictionary<string, SkillNodeDefinition> _nodes;

    public static void LoadAll()
    {
        if (_nodes != null) return;
        _nodes = new Dictionary<string, SkillNodeDefinition>();
        var assets = Resources.LoadAll<SkillNodeDefinition>("");
        for (int i = 0; i < assets.Length; i++)
        {
            var node = assets[i];
            if (node != null && !string.IsNullOrEmpty(node.id))
            {
                _nodes[node.id] = node;
            }
        }
    }

    public static SkillNodeDefinition Get(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        LoadAll();
        _nodes.TryGetValue(id, out var node);
        return node;
    }

    public static IEnumerable<SkillNodeDefinition> All
    {
        get
        {
            LoadAll();
            return _nodes.Values;
        }
    }
}
