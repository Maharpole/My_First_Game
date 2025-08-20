using System;
using System.Collections.Generic;
using UnityEngine;

public static class SkillTreeState
{
    const string SaveKey = "skilltree.unlocked.ids";
    static HashSet<string> _unlocked;
    public static event Action<SkillNodeData> OnUnlocked;

    // Verbose logging to help diagnose unlock flow and toggle behavior
    static bool _verboseLogging = true; // set to false to silence logs
    static void Log(string message)
    {
        if (_verboseLogging) Debug.Log("[SkillTreeState] " + message);
    }

    static HashSet<string> Unlocked
    {
        get
        {
            if (_unlocked != null) return _unlocked;
            _unlocked = new HashSet<string>();
            var csv = PlayerPrefs.GetString(SaveKey, string.Empty);
            if (!string.IsNullOrEmpty(csv))
            {
                var parts = csv.Split('|');
                for (int i = 0; i < parts.Length; i++) if (!string.IsNullOrEmpty(parts[i])) _unlocked.Add(parts[i]);
            }
            return _unlocked;
        }
    }

    public static bool IsUnlocked(SkillNodeData node) => node != null && !string.IsNullOrEmpty(node.id) && Unlocked.Contains(node.id);

    public static bool ParentsSatisfied(SkillNodeData node)
    {
        if (node == null)
        {
            Log("ParentsSatisfied: node=null → false");
            return false;
        }
        if (node.parents == null || node.parents.Count == 0) return true;
        for (int i = 0; i < node.parents.Count; i++)
        {
            if (!IsUnlocked(node.parents[i]))
            {
                Log($"ParentsSatisfied: parent '{(node.parents[i] != null ? node.parents[i].id : "<null>")}' not unlocked for '{node.id}'");
                return false;
            }
        }
        return true;
    }

    public static int RemainingPoints => PlayerProfile.UnspentSkillPoints;
    public static int SpentPoints => Unlocked.Count; // assumes cost=1 per node
    public static int TotalPoints => RemainingPoints + SpentPoints;

    public static bool CanUnlock(SkillNodeData node)
    {
        string id = node != null ? node.id : "<null>";
        if (node == null)
        {
            Log("CanUnlock: node is null");
            return false;
        }
        if (IsUnlocked(node))
        {
            Log($"CanUnlock: '{id}' already unlocked");
            return false;
        }
        bool parentsOk = ParentsSatisfied(node);
        int cost = Mathf.Max(1, node.cost);
        int points = RemainingPoints;
        Log($"CanUnlock: '{id}' parentsOk={parentsOk} points={points} cost={cost}");
        if (!parentsOk) return false;
        return points >= cost;
    }

    public static bool TryUnlock(SkillNodeData node)
    {
        if (!CanUnlock(node))
        {
            Log($"TryUnlock: denied for '{(node != null ? node.id : "<null>")}'");
            return false;
        }
        int cost = Mathf.Max(1, node.cost);
        PlayerProfile.UnspentSkillPoints = RemainingPoints - cost;
        Unlocked.Add(node.id);
        Log($"TryUnlock: success for '{node.id}', spent={cost}, remaining={PlayerProfile.UnspentSkillPoints}");
        Save();
        OnUnlocked?.Invoke(node);
        return true;
    }

    static void Save()
    {
        PlayerPrefs.SetString(SaveKey, string.Join("|", Unlocked));
        Log($"Save: persisted {Unlocked.Count} unlocked ids");
        PlayerPrefs.Save();
    }

    public static void ClearAllUnlocks()
    {
        if (_unlocked != null) _unlocked.Clear();
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
        Log("ClearAllUnlocks: removed persisted unlocks");
    }

    // Save/Load helpers for external save system
    public static string ExportCsv()
    {
        return string.Join("|", Unlocked);
    }

    public static void ImportCsv(string csv, bool replace = true)
    {
        if (replace && _unlocked != null) _unlocked.Clear();
        var target = Unlocked;
        if (!string.IsNullOrEmpty(csv))
        {
            var parts = csv.Split('|');
            for (int i = 0; i < parts.Length; i++) if (!string.IsNullOrEmpty(parts[i])) target.Add(parts[i]);
        }
        Save();
        Log($"ImportCsv: loaded {_unlocked.Count} ids");
    }

    static int TotalCost()
    {
        // If needed we could also persist a total cost; for now approximate by counting ids
        return Unlocked.Count; // assumes cost=1 for all nodes
    }
}


