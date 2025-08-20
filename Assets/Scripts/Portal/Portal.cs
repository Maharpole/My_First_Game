using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class Portal : MonoBehaviour
{
    [Header("Portal Settings")]
    [Tooltip("Name of the scene to teleport to")]
    public string targetSceneName;

    [Tooltip("Optional spawn point id in the target scene. If set, the player will appear at the matching PlayerSpawnPoint.")]
    public string targetSpawnId = "";

    [Tooltip("Time in seconds the player must stay in the portal to trigger teleportation")]
    public float teleportDelay = 2f;

    [Tooltip("Distance at which the player is considered to be in the portal")]
    public float triggerDistance = 2f;

    [Tooltip("Visual effect to show teleportation progress (optional)")]
    public GameObject progressEffect;

    [Header("Use Action (New Input System)")]
    [Tooltip("If true, requires pressing the useAction while in range to teleport. If false, uses teleportDelay.")]
    public bool requireUseAction = true;
#if ENABLE_INPUT_SYSTEM
    [Tooltip("Input action to use the portal when in range (performed triggers teleport).")]
    public InputActionReference useAction;
#endif

    [Header("Debug")]
    [Tooltip("Enable debug logs")]
    public bool enableDebugLogs = false;

    private bool playerInPortal = false;
    private float timeInPortal = 0f;
    private GameObject player;
    private bool isTeleporting = false;
    private Collider portalCollider;

    private void Start()
    {
        // Try to find the player
        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning("Player not found! Portal will not work until player is found.");
            }
        }
        else
        {
            // Check player setup
            CheckPlayerSetup();
        }

        // Ensure the collider is set up correctly
        portalCollider = GetComponent<Collider>();
        if (portalCollider == null)
        {
            if (enableDebugLogs)
            {
                Debug.LogError("No collider found on Portal! Adding a BoxCollider.");
            }
            portalCollider = gameObject.AddComponent<BoxCollider>();
        }
        
        // Make sure the collider is set as a trigger
        portalCollider.isTrigger = true;

        if (enableDebugLogs)
        {
            Debug.Log($"Portal initialized with collider: {portalCollider.GetType().Name}, IsTrigger: {portalCollider.isTrigger}");
            Debug.Log($"Portal Layer: {gameObject.layer} ({LayerMask.LayerToName(gameObject.layer)})");
        }
    }

#if ENABLE_INPUT_SYSTEM
    private void OnEnable()
    {
        if (requireUseAction && useAction != null)
        {
            useAction.action.performed += OnUsePerformed;
            useAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (requireUseAction && useAction != null)
        {
            useAction.action.performed -= OnUsePerformed;
            useAction.action.Disable();
        }
    }
#endif

    private void CheckPlayerSetup()
    {
        if (enableDebugLogs)
        {
            // Check player collider
            Collider playerCollider = player.GetComponent<Collider>();
            if (playerCollider == null)
            {
                Debug.LogError("Player has no collider! This will prevent portal triggers from working.");
            }
            else
            {
                Debug.Log($"Player collider found: {playerCollider.GetType().Name}, IsTrigger: {playerCollider.isTrigger}");
            }

            // Check layers
            Debug.Log($"Player Layer: {player.layer} ({LayerMask.LayerToName(player.layer)})");
            Debug.Log($"Can layers interact? {Physics.GetIgnoreLayerCollision(player.layer, gameObject.layer) == false}");
        }
    }

    private void Update()
    {
        // Check if player exists
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;
        }

        // Check distance to player
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        
        // Check if player is within trigger distance
        bool wasInPortal = playerInPortal;
        playerInPortal = distanceToPlayer <= triggerDistance;

        // Handle portal entry/exit
        if (playerInPortal && !wasInPortal)
        {
            // Player just entered portal
            if (enableDebugLogs)
            {
                Debug.Log($"Player entered portal range. Distance: {distanceToPlayer}, Trigger distance: {triggerDistance}");
            }
            timeInPortal = 0f;
        }
        else if (!playerInPortal && wasInPortal)
        {
            // Player just exited portal
            if (enableDebugLogs)
            {
                Debug.Log($"Player exited portal range. Distance: {distanceToPlayer}, Trigger distance: {triggerDistance}");
            }
            timeInPortal = 0f;
            
            // Reset progress effect if assigned
            if (progressEffect != null)
            {
                progressEffect.transform.localScale = Vector3.zero;
            }
        }

        // Update teleportation progress (only when not requiring explicit use)
        if (!requireUseAction && playerInPortal && !isTeleporting)
        {
            timeInPortal += Time.deltaTime;

            // Update progress effect if assigned
            if (progressEffect != null)
            {
                // Assuming the progress effect has a scale or fill property
                float progress = timeInPortal / teleportDelay;
                progressEffect.transform.localScale = new Vector3(progress, progress, progress);
            }

            if (timeInPortal >= teleportDelay)
            {
                StartTeleportation();
            }
        }
    }

#if ENABLE_INPUT_SYSTEM
    private void OnUsePerformed(InputAction.CallbackContext _)
    {
        if (!requireUseAction || isTeleporting) return;
        if (!playerInPortal) return;
        StartTeleportation();
    }
#endif

    private void OnTriggerEnter(Collider other)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"Trigger entered by: {other.gameObject.name} (Tag: {other.tag}, Layer: {LayerMask.LayerToName(other.gameObject.layer)})");
        }

        // This is now a backup method in case the distance check fails
        if (other.CompareTag("Player"))
        {
            playerInPortal = true;
            timeInPortal = 0f;

            if (enableDebugLogs)
            {
                Debug.Log($"Player entered portal via trigger. Player position: {other.transform.position}, Portal position: {transform.position}");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"Trigger exited by: {other.gameObject.name} (Tag: {other.tag}, Layer: {LayerMask.LayerToName(other.gameObject.layer)})");
        }

        // This is now a backup method in case the distance check fails
        if (other.CompareTag("Player"))
        {
            playerInPortal = false;
            timeInPortal = 0f;

            // Reset progress effect if assigned
            if (progressEffect != null)
            {
                progressEffect.transform.localScale = Vector3.zero;
            }

            if (enableDebugLogs)
            {
                Debug.Log($"Player exited portal via trigger. Player position: {other.transform.position}, Portal position: {transform.position}");
            }
        }
    }

    private void StartTeleportation()
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            if (enableDebugLogs)
            {
                Debug.LogError("Target scene name not set!");
            }
            return;
        }

        isTeleporting = true;

        if (enableDebugLogs)
        {
            Debug.Log($"Teleporting to scene: {targetSceneName}");
        }

        // Inform the spawn resolver where to place the player in the next scene
        PortalSpawnData.SetNext(targetSceneName, targetSpawnId);

        // Start the teleportation coroutine
        StartCoroutine(TeleportToScene());
    }

    private IEnumerator TeleportToScene()
    {
        // Optional: Add a fade out effect here
        // yield return StartCoroutine(FadeOut());

        // Load the new scene
        SceneManager.LoadScene(targetSceneName);

        yield return null;
    }

    // Optional: Add visual gizmos to help with setup in the editor
    private void OnDrawGizmos()
    {
        // Draw the portal bounds
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, GetComponent<Collider>().bounds.size);
        
        // Draw the trigger distance
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, triggerDistance);
    }
}