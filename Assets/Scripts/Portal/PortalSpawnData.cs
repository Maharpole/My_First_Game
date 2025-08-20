using UnityEngine;
using UnityEngine.SceneManagement;

public static class PortalSpawnData
{
    private static string _nextSceneName;
    private static string _nextSpawnId;

    public static void SetNext(string sceneName, string spawnId)
    {
        _nextSceneName = sceneName;
        _nextSpawnId = spawnId;
    }

    public static bool TryConsume(out string sceneName, out string spawnId)
    {
        sceneName = _nextSceneName;
        spawnId = _nextSpawnId;
        bool has = !string.IsNullOrEmpty(sceneName);
        _nextSceneName = null;
        _nextSpawnId = null;
        return has;
    }
}


