using UnityEditor;
using UnityEngine;

public static class CleanMissingScripts
{
#if UNITY_EDITOR
    [MenuItem("Tools/Project/Clean Missing Scripts In Open Scenes")]
    public static void CleanInOpenScenes()
    {
        int totalRemoved = 0;
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;
            foreach (var root in scene.GetRootGameObjects())
            {
                totalRemoved += RemoveMissingRecursively(root);
            }
        }
        EditorUtility.DisplayDialog("Clean Missing Scripts",
            $"Removed {totalRemoved} missing script component(s) from open scenes.", "OK");
    }

    [MenuItem("Tools/Project/Clean Missing Scripts In Selected Prefabs")] 
    public static void CleanInSelectedPrefabs()
    {
        int totalRemoved = 0;
        foreach (var obj in Selection.gameObjects)
        {
            totalRemoved += RemoveMissingRecursively(obj);
            EditorUtility.SetDirty(obj);
        }
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Clean Missing Scripts",
            $"Removed {totalRemoved} missing script component(s) from selected prefabs.", "OK");
    }

    static int RemoveMissingRecursively(GameObject go)
    {
        int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
        for (int i = 0; i < go.transform.childCount; i++)
        {
            removed += RemoveMissingRecursively(go.transform.GetChild(i).gameObject);
        }
        return removed;
    }
#endif
}


