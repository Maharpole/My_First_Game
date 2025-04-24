 using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class Portal : MonoBehaviour
{
    [Header("Portal Settings")]
    [Tooltip("Name of the scene to teleport to")]
    public string targetSceneName;

    [Tooltip("Time in seconds the player must stay in the portal to trigger teleportation")]
    public float teleportDelay = 2f;

    [Tooltip("Visual effect to show teleportation progress (optional)")]
    public GameObject progressEffect;

    [Header("Debug")]
    [Tooltip("Enable debug logs")]
    public bool enableDebugLogs = false;

    private bool playerInPortal = false;
    private float timeInPortal = 0f;
    private GameObject player;
    private bool isTeleporting = false;

    private void Start()
    {
        // Try to find the player
        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("Player not found! Portal will not work until player is found.");
        }
    }

    private void Update()
    {
        if (playerInPortal && !isTeleporting)
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

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInPortal = true;
            timeInPortal = 0f;

            if (enableDebugLogs)
            {
                Debug.Log("Player entered portal");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
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
                Debug.Log("Player exited portal");
            }
        }
    }

    private void StartTeleportation()
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError("Target scene name not set!");
            return;
        }

        isTeleporting = true;

        if (enableDebugLogs)
        {
            Debug.Log($"Teleporting to scene: {targetSceneName}");
        }

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
    }
}