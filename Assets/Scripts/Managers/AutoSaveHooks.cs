using UnityEngine;
using UnityEngine.SceneManagement;

// Attach to a bootstrap object in every scene; it autosaves on character creation and scene switches.
public class AutoSaveHooks : MonoBehaviour
{
    void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        // Save on first scene start once Player is initialized
        Invoke(nameof(SaveOnceReady), 0.25f);
    }

    void SaveOnceReady()
    {
        var player = FindFirstObjectByType<Player>();
        if (player != null)
        {
            SaveSystem.SavePlayer(player);
        }
        else
        {
            // retry shortly if Player spawns later
            Invoke(nameof(SaveOnceReady), 0.25f);
        }
    }

    void OnSceneLoaded(Scene s, LoadSceneMode mode)
    {
        // Save immediately after each scene load (player persists across scenes)
        var player = FindFirstObjectByType<Player>();
        if (player != null) SaveSystem.SavePlayer(player);
    }
}


