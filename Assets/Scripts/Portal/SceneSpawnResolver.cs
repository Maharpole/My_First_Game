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
                    Vector3 spawnPos = target.Position;
                    Quaternion spawnRot = target.Rotation;

                    // If agent exists, warp first
                    var agent = player.GetComponent<UnityEngine.AI.NavMeshAgent>();
                    if (agent != null)
                    {
                        // Ensure base offset is zero so vertical placement is controlled here
                        agent.baseOffset = 0f;
                        agent.Warp(spawnPos);
                        t.rotation = spawnRot;
                    }
                    else
                    {
                        t.SetPositionAndRotation(spawnPos, spawnRot);
                    }

                    // Snap to ground next frame after everything initializes
                    player.StartCoroutine(SnapToGroundNextFrame(player));
                }
            }
        }
    }

    private System.Collections.IEnumerator SnapToGroundNextFrame(Player player)
    {
        yield return null; // wait one frame

        if (player == null) yield break;
        var root = player.transform;

        // Prefer collider bounds for bottom, fallback to renderer bounds
        var anyCol = player.GetComponent<Collider>() ?? player.GetComponentInChildren<Collider>();
        var renderers = player.GetComponentsInChildren<Renderer>();
        bool hasRenderers = renderers != null && renderers.Length > 0;
        float bottomWorldY = float.NaN;
        if (anyCol != null)
        {
            bottomWorldY = anyCol.bounds.min.y;
        }
        else if (hasRenderers)
        {
            float minY = float.PositiveInfinity;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                var b = renderers[i].bounds;
                if (b.size.sqrMagnitude <= 0f) continue;
                if (b.min.y < minY) minY = b.min.y;
            }
            bottomWorldY = minY;
        }

        // Determine cast height above current position
        float castHeight = 3f;
        if (anyCol is CapsuleCollider cap)
        {
            castHeight = Mathf.Max(castHeight, cap.height);
        }
        else if (anyCol is CharacterController cc)
        {
            castHeight = Mathf.Max(castHeight, cc.height);
        }

        Vector3 origin = root.position + Vector3.up * castHeight;
        if (Physics.Raycast(origin, Vector3.down, out var hit, castHeight * 2f, ~0, QueryTriggerInteraction.Ignore))
        {
            const float skin = 0.01f;
            if (!float.IsNaN(bottomWorldY) && bottomWorldY < float.PositiveInfinity)
            {
                float delta = (hit.point.y + skin) - bottomWorldY;
                root.position += new Vector3(0f, delta, 0f);
            }
            else
            {
                root.position = new Vector3(root.position.x, hit.point.y + skin, root.position.z);
            }
        }
        else
        {
            // Fallback: snap to NavMesh height if available
            if (UnityEngine.AI.NavMesh.SamplePosition(root.position, out var navHit, 5f, UnityEngine.AI.NavMesh.AllAreas))
            {
                root.position = navHit.position;
            }
        }
    }
}


