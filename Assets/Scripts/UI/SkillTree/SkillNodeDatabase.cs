using System.Collections.Generic;
using UnityEngine;

public static class SkillNodeDatabase
{
    static Dictionary<string, SkillNodeData> _nodes;

    public static void LoadAll()
    {
        if (_nodes != null) return;
        _nodes = new Dictionary<string, SkillNodeData>();
        var assets = Resources.LoadAll<SkillNodeData>("");
        for (int i = 0; i < assets.Length; i++)
        {
            var node = assets[i];
            if (node != null && !string.IsNullOrEmpty(node.id))
            {
                _nodes[node.id] = node;
            }
        }
    }

    public static SkillNodeData Get(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        LoadAll();
        _nodes.TryGetValue(id, out var node);
        return node;
    }

    public static IEnumerable<SkillNodeData> All
    {
        get
        {
            LoadAll();
            return _nodes.Values;
        }
    }
}
