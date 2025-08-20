using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PortalSpawner : MonoBehaviour
{
    [Header("Spawn")] public GameObject portalPrefab;
    public Vector3 spawnOffset = new Vector3(0f, 0f, 2.5f);
    [Tooltip("Default scene to assign to spawned portals")] public string defaultTargetScene;
    [Tooltip("Default spawn id in the target scene for spawned portals (optional)")]
    public string defaultTargetSpawnId = "";

#if ENABLE_INPUT_SYSTEM
    [Header("Input (New Input System)")]
    [Tooltip("Action to spawn a portal in front of the player")] public InputActionReference spawnPortalAction;
#endif

    private Transform _player;

    void Awake()
    {
        var p = Player.Instance ?? FindFirstObjectByType<Player>();
        _player = p != null ? p.transform : null;
    }

#if ENABLE_INPUT_SYSTEM
    void OnEnable()
    {
        if (spawnPortalAction != null)
        {
            spawnPortalAction.action.performed += OnSpawnPerformed;
            spawnPortalAction.action.Enable();
        }
    }

    void OnDisable()
    {
        if (spawnPortalAction != null)
        {
            spawnPortalAction.action.performed -= OnSpawnPerformed;
            spawnPortalAction.action.Disable();
        }
    }
#endif

#if ENABLE_INPUT_SYSTEM
    void OnSpawnPerformed(InputAction.CallbackContext _)
    {
        SpawnPortal();
    }
#endif

    public void SpawnPortal()
    {
        if (portalPrefab == null)
        {
            Debug.LogWarning("[PortalSpawner] No portalPrefab assigned");
            return;
        }
        if (_player == null)
        {
            var p = Player.Instance ?? FindFirstObjectByType<Player>();
            _player = p != null ? p.transform : null;
            if (_player == null)
            {
                Debug.LogWarning("[PortalSpawner] Player not found");
                return;
            }
        }
        var forward = _player.forward;
        forward.y = 0f;
        forward.Normalize();
        var pos = _player.position + forward * spawnOffset.z + Vector3.up * spawnOffset.y + _player.right * spawnOffset.x;
        var rot = Quaternion.LookRotation(forward != Vector3.zero ? forward : Vector3.forward, Vector3.up);
        var go = Instantiate(portalPrefab, pos, rot);
        var portal = go.GetComponent<Portal>();
        if (portal != null && !string.IsNullOrEmpty(defaultTargetScene))
        {
            portal.targetSceneName = defaultTargetScene;
            portal.requireUseAction = true; // default to explicit use
            if (!string.IsNullOrEmpty(defaultTargetSpawnId))
            {
                portal.targetSpawnId = defaultTargetSpawnId;
            }
        }
    }
}


