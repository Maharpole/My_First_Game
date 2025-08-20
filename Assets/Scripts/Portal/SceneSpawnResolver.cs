using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSpawnResolver : MonoBehaviour
{
    [Tooltip("If true, persists across scenes to always reposition the player on load.")]
    public bool dontDestroyOnLoad = true;

    [Tooltip("Fallback spawn id to use if a portal didn't specify one.")]
    public string defaultSpawnId = "";

    void Awake()
    {
        if (dontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!PortalSpawnData.TryConsume(out var nextScene, out var spawnId))
        {
            spawnId = defaultSpawnId;
        }

        // If a spawnId is available, place the player there
        if (!string.IsNullOrEmpty(spawnId))
        {
            var spawnPoints = Object.FindObjectsByType<PlayerSpawnPoint>(FindObjectsSortMode.None);
            PlayerSpawnPoint target = null;
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] != null && spawnPoints[i].spawnId == spawnId)
                {
                    target = spawnPoints[i];
                    break;
                }
            }
            if (target != null)
            {
                var player = Player.Instance ?? Object.FindFirstObjectByType<Player>();
                if (player != null)
                {
                    var t = player.transform;
                    t.position = target.Position;
                    t.rotation = target.Rotation;

                    // Try moving NavMeshAgent if any to avoid warp issues
                    var agent = player.GetComponent<UnityEngine.AI.NavMeshAgent>();
                    if (agent != null)
                    {
                        agent.Warp(target.Position);
                        t.rotation = target.Rotation;
                    }
                }
            }
        }
    }
}


