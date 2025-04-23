using UnityEngine;

/// <summary>
/// Automatically registers this GameObject as the pause menu panel with the PauseManager
/// </summary>
public class PauseMenuRegistrar : MonoBehaviour
{
    [Tooltip("Automatically add the PauseMenu tag if missing")]
    public bool autoAddTag = true;
    
    [Tooltip("Delay registration to ensure PauseManager is initialized")]
    public bool delayRegistration = true;
    
    [Tooltip("The actual panel to show/hide when pausing (if null, will use this GameObject)")]
    public GameObject pausePanel;
    
    private void Awake()
    {
        // Add the PauseMenu tag if needed
        if (autoAddTag && gameObject.tag != "PauseMenu")
        {
            Debug.Log("PauseMenuRegistrar: Adding 'PauseMenu' tag to " + gameObject.name);
            gameObject.tag = "PauseMenu";
        }
        
        // If no panel is assigned, try to find a child panel
        if (pausePanel == null)
        {
            // Look for a child with "Panel" in the name
            foreach (Transform child in transform)
            {
                if (child.name.Contains("Panel"))
                {
                    pausePanel = child.gameObject;
                    Debug.Log("PauseMenuRegistrar: Found panel child: " + pausePanel.name);
                    break;
                }
            }
            
            // If still null, use this GameObject
            if (pausePanel == null)
            {
                pausePanel = gameObject;
                Debug.Log("PauseMenuRegistrar: No panel found, using this GameObject: " + gameObject.name);
            }
        }
    }
    
    private void Start()
    {
        if (delayRegistration)
        {
            // Delay registration to ensure PauseManager is initialized
            Invoke("RegisterWithPauseManager", 0.1f);
        }
        else
        {
            RegisterWithPauseManager();
        }
    }
    
    private void RegisterWithPauseManager()
    {
        // Make sure this GameObject has the PauseMenu tag
        if (gameObject.tag != "PauseMenu")
        {
            Debug.LogWarning("PauseMenuRegistrar: This GameObject should have the 'PauseMenu' tag");
        }
        
        // Register with PauseManager
        if (PauseManager.Instance != null)
        {
            // Ensure the panel is properly set up before registering
            EnsurePanelIsProperlySetUp();
            
            // Register the panel, not the canvas
            PauseManager.Instance.SetPauseMenuPanel(pausePanel);
            Debug.Log("PauseMenuRegistrar: Successfully registered panel " + pausePanel.name + " with PauseManager");
        }
        else
        {
            Debug.LogError("PauseMenuRegistrar: PauseManager instance not found");
        }
    }
    
    private void EnsurePanelIsProperlySetUp()
    {
        // Make sure the panel is active
        if (!pausePanel.activeInHierarchy)
        {
            Debug.Log("PauseMenuRegistrar: Activating panel " + pausePanel.name);
            pausePanel.SetActive(true);
        }
        
        // Check if the panel has a Canvas component and ensure it's enabled
        Canvas canvas = pausePanel.GetComponent<Canvas>();
        if (canvas != null)
        {
            Debug.Log("PauseMenuRegistrar: Ensuring Canvas component on " + pausePanel.name + " is enabled");
            canvas.enabled = true;
        }
        
        // Check if the panel has a CanvasGroup component and ensure it's visible
        CanvasGroup canvasGroup = pausePanel.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            Debug.Log("PauseMenuRegistrar: Setting CanvasGroup properties on " + pausePanel.name);
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        
        // Make sure all parent objects are active
        Transform parent = pausePanel.transform.parent;
        while (parent != null)
        {
            if (!parent.gameObject.activeInHierarchy)
            {
                Debug.Log("PauseMenuRegistrar: Activating parent " + parent.name + " of " + pausePanel.name);
                parent.gameObject.SetActive(true);
            }
            parent = parent.parent;
        }
    }
} 