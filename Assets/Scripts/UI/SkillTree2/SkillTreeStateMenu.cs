using UnityEditor;

public static class SkillTreeStateMenu
{
#if UNITY_EDITOR
    [MenuItem("Tools/Skill Tree/Clear All Unlocks")] 
    public static void MenuClear()
    {
        SkillTreeState.ClearAllUnlocks();
        EditorUtility.DisplayDialog("Skill Tree", "Cleared all saved unlocks.", "OK");
    }
#endif
}


