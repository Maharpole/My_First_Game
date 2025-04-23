using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [Header("Pause Settings")]
    [Tooltip("Key to toggle pause menu")]
    public KeyCode pauseKey = KeyCode.Escape;
    
    [Tooltip("Whether the game starts paused")]
    public bool startPaused = false;
    
    [Header("UI References")]
    [Tooltip("Reference to the pause menu panel")]
    public GameObject pauseMenuPanel;
    
    [Tooltip("Tag to find the pause menu panel in each scene")]
    public string pauseMenuTag = "PauseMenu";
    
    [Tooltip("Reference to the main menu scene name")]
    public string mainMenuSceneName = "MainMenu";
    
    [Header("Debug")]
    [Tooltip("Enable detailed debug logs")]
    public bool enableDebugLogs = true;
    
    // Singleton instance
    public static PauseManager Instance { get; private set; }
    
    // Current pause state
    private bool isPaused = false;
    
    // Flag to track if we've already found the panel in this scene
    private bool panelFoundInCurrentScene = false;
    
    private void Awake()
    {
        DebugLog("PauseManager Awake called");
        
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            DebugLog("PauseManager singleton initialized");
            
            // Subscribe to scene loaded event
            SceneManager.sceneLoaded += OnSceneLoaded;
            DebugLog("Subscribed to scene loaded event");
        }
        else
        {
            DebugLog("Destroying duplicate PauseManager");
            Destroy(gameObject);
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from scene loaded event
        SceneManager.sceneLoaded -= OnSceneLoaded;
        DebugLog("Unsubscribed from scene loaded event");
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        DebugLog($"Scene loaded: {scene.name}, mode: {mode}");
        
        // Reset the panel found flag for the new scene
        panelFoundInCurrentScene = false;
        DebugLog("Reset panel found flag for new scene");
        
        // Find the pause menu panel in the new scene
        FindPauseMenuPanel();
        
        // Reset pause state when a new scene is loaded
        isPaused = false;
        Time.timeScale = 1f;
        DebugLog("Reset pause state for new scene");
        
        // Hide pause menu
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
            DebugLog("Hide pause menu panel for new scene");
        }
        else
        {
            DebugLogWarning("No pause menu panel to hide for new scene");
        }
    }
    
    private void FindPauseMenuPanel()
    {
        DebugLog("Finding pause menu panel...");
        
        // If we've already found the panel in this scene, don't look again
        if (panelFoundInCurrentScene && pauseMenuPanel != null)
        {
            DebugLog("Panel already found in current scene, skipping search");
            return;
        }
        
        // Try to find the pause menu panel by tag
        GameObject panel = GameObject.FindGameObjectWithTag(pauseMenuTag);
        
        if (panel != null)
        {
            pauseMenuPanel = panel;
            panelFoundInCurrentScene = true;
            DebugLog($"Found pause menu panel by tag '{pauseMenuTag}': {panel.name}");
        }
        else
        {
            DebugLog($"No GameObject found with tag '{pauseMenuTag}'");
            
            // Try to find by name as fallback
            panel = GameObject.Find("PauseMenuPanel");
            if (panel != null)
            {
                pauseMenuPanel = panel;
                panelFoundInCurrentScene = true;
                DebugLog($"Found pause menu panel by name 'PauseMenuPanel': {panel.name}");
            }
            else
            {
                DebugLog("No GameObject found with name 'PauseMenuPanel'");
                
                // Try to find any panel with PauseMenuUI component
                PauseMenuUI[] pauseMenus = FindObjectsOfType<PauseMenuUI>();
                if (pauseMenus.Length > 0)
                {
                    pauseMenuPanel = pauseMenus[0].gameObject;
                    panelFoundInCurrentScene = true;
                    DebugLog($"Found pause menu panel by PauseMenuUI component: {pauseMenus[0].gameObject.name}");
                }
                else
                {
                    DebugLog("No GameObject found with PauseMenuUI component");
                    
                    // Try to find any panel with PauseMenuRegistrar component
                    PauseMenuRegistrar[] registrars = FindObjectsOfType<PauseMenuRegistrar>();
                    if (registrars.Length > 0)
                    {
                        pauseMenuPanel = registrars[0].gameObject;
                        panelFoundInCurrentScene = true;
                        DebugLog($"Found pause menu panel by PauseMenuRegistrar component: {registrars[0].gameObject.name}");
                    }
                    else
                    {
                        DebugLogWarning("Pause menu panel not found in the new scene. Make sure it has the tag '" + pauseMenuTag + "' or is named 'PauseMenuPanel'");
                        pauseMenuPanel = null;
                    }
                }
            }
        }
        
        // Log the current state of the pause menu panel
        if (pauseMenuPanel != null)
        {
            DebugLog($"Pause menu panel reference: {pauseMenuPanel.name}, active: {pauseMenuPanel.activeInHierarchy}, tag: {pauseMenuPanel.tag}");
        }
        else
        {
            DebugLogWarning("Pause menu panel reference is null");
        }
    }
    
    private void Start()
    {
        DebugLog("PauseManager Start called");
        
        // Find the pause menu panel in the initial scene
        FindPauseMenuPanel();
        
        // Initialize pause state
        if (startPaused)
        {
            DebugLog("Game starts paused");
            PauseGame();
        }
        else
        {
            DebugLog("Game starts unpaused");
            ResumeGame();
        }
    }
    
    private void Update()
    {
        // Check for pause key press
        if (Input.GetKeyDown(pauseKey))
        {
            DebugLog($"Pause key ({pauseKey}) pressed");
            TogglePause();
        }
    }
    
    /// <summary>
    /// Toggles the pause state of the game
    /// </summary>
    public void TogglePause()
    {
        DebugLog($"TogglePause called, current state: {(isPaused ? "paused" : "unpaused")}");
        
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }
    
    /// <summary>
    /// Pauses the game
    /// </summary>
    public void PauseGame()
    {
        DebugLog("PauseGame called");
        
        isPaused = true;
        Time.timeScale = 0f;
        DebugLog("Time scale set to 0");
        
        // Make sure we have a reference to the pause menu panel
        if (pauseMenuPanel == null)
        {
            DebugLog("Pause menu panel reference is null, trying to find it");
            FindPauseMenuPanel();
        }
        
        // Show pause menu
        if (pauseMenuPanel != null)
        {
            DebugLog($"Showing pause menu panel: {pauseMenuPanel.name}, active before: {pauseMenuPanel.activeInHierarchy}");
            
            // Make sure the panel and all its parents are active
            SetPanelAndParentsActive(pauseMenuPanel, true);
            
            // Force the panel to be visible by ensuring it's active in the hierarchy
            if (!pauseMenuPanel.activeInHierarchy)
            {
                DebugLogWarning($"Panel {pauseMenuPanel.name} is still inactive after SetPanelAndParentsActive. Forcing activation.");
                pauseMenuPanel.SetActive(true);
                
                // Check if the panel has a Canvas component and ensure it's enabled
                Canvas canvas = pauseMenuPanel.GetComponent<Canvas>();
                if (canvas != null)
                {
                    DebugLog($"Found Canvas component on {pauseMenuPanel.name}, ensuring it's enabled");
                    canvas.enabled = true;
                }
                
                // Check if the panel has a CanvasGroup component and ensure it's visible
                CanvasGroup canvasGroup = pauseMenuPanel.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    DebugLog($"Found CanvasGroup component on {pauseMenuPanel.name}, setting alpha to 1");
                    canvasGroup.alpha = 1f;
                    canvasGroup.interactable = true;
                    canvasGroup.blocksRaycasts = true;
                }
            }
            
            DebugLog($"Pause menu panel active after: {pauseMenuPanel.activeInHierarchy}");
        }
        else
        {
            DebugLogWarning("Cannot show pause menu: panel reference is missing");
        }
        
        // Notify other systems that the game is paused
        OnGamePaused();
    }
    
    /// <summary>
    /// Resumes the game
    /// </summary>
    public void ResumeGame()
    {
        DebugLog("ResumeGame called");
        
        isPaused = false;
        Time.timeScale = 1f;
        DebugLog("Time scale set to 1");
        
        // Make sure we have a reference to the pause menu panel
        if (pauseMenuPanel == null)
        {
            DebugLog("Pause menu panel reference is null, trying to find it");
            FindPauseMenuPanel();
        }
        
        // Hide pause menu
        if (pauseMenuPanel != null)
        {
            DebugLog($"Hiding pause menu panel: {pauseMenuPanel.name}, active before: {pauseMenuPanel.activeInHierarchy}");
            pauseMenuPanel.SetActive(false);
            DebugLog($"Pause menu panel active after: {pauseMenuPanel.activeInHierarchy}");
        }
        
        // Notify other systems that the game is resumed
        OnGameResumed();
    }
    
    /// <summary>
    /// Sets a GameObject and all its parents to active/inactive
    /// </summary>
    private void SetPanelAndParentsActive(GameObject panel, bool active)
    {
        if (panel == null) return;
        
        // First, make sure all parents are active
        Transform parent = panel.transform.parent;
        while (parent != null)
        {
            if (!parent.gameObject.activeInHierarchy)
            {
                DebugLog($"Activating parent: {parent.name}");
                parent.gameObject.SetActive(true);
            }
            parent = parent.parent;
        }
        
        // Then set the panel itself active
        panel.SetActive(active);
    }
    
    /// <summary>
    /// Returns to the main menu
    /// </summary>
    public void ReturnToMainMenu()
    {
        DebugLog("ReturnToMainMenu called");
        
        // Resume time before loading new scene
        Time.timeScale = 1f;
        DebugLog("Time scale set to 1 before loading main menu");
        
        // Load main menu scene
        DebugLog($"Loading main menu scene: {mainMenuSceneName}");
        SceneManager.LoadScene(mainMenuSceneName);
    }
    
    /// <summary>
    /// Quits the game
    /// </summary>
    public void QuitGame()
    {
        DebugLog("QuitGame called");
        
        #if UNITY_EDITOR
            DebugLog("Quitting game in editor");
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            DebugLog("Quitting game in build");
            Application.Quit();
        #endif
    }
    
    /// <summary>
    /// Called when the game is paused
    /// </summary>
    private void OnGamePaused()
    {
        DebugLog("OnGamePaused called");
        // Notify other systems that the game is paused
        // For example, pause audio, animations, etc.
    }
    
    /// <summary>
    /// Called when the game is resumed
    /// </summary>
    private void OnGameResumed()
    {
        DebugLog("OnGameResumed called");
        // Notify other systems that the game is resumed
        // For example, resume audio, animations, etc.
    }
    
    /// <summary>
    /// Returns whether the game is currently paused
    /// </summary>
    public bool IsPaused()
    {
        return isPaused;
    }
    
    /// <summary>
    /// Manually sets the pause menu panel reference
    /// </summary>
    public void SetPauseMenuPanel(GameObject panel)
    {
        DebugLog($"SetPauseMenuPanel called with panel: {(panel != null ? panel.name : "null")}");
        
        if (panel != null)
        {
            pauseMenuPanel = panel;
            panelFoundInCurrentScene = true;
            DebugLog($"Manually set pause menu panel reference to: {panel.name}, active: {panel.activeInHierarchy}, tag: {panel.tag}");
            
            // Log the hierarchy of the panel for debugging
            LogPanelHierarchy(panel);
        }
        else
        {
            DebugLogWarning("SetPauseMenuPanel called with null panel");
        }
    }
    
    private void LogPanelHierarchy(GameObject panel)
    {
        if (panel == null) return;
        
        DebugLog($"Panel hierarchy for {panel.name}:");
        Transform current = panel.transform;
        string hierarchy = panel.name;
        
        while (current.parent != null)
        {
            current = current.parent;
            hierarchy = current.name + " -> " + hierarchy;
        }
        
        DebugLog($"Full hierarchy: {hierarchy}");
        
        // Check if the panel is active in the hierarchy
        if (!panel.activeInHierarchy)
        {
            DebugLogWarning($"Panel {panel.name} is not active in hierarchy!");
            
            // Find which parent is inactive
            current = panel.transform;
            while (current.parent != null)
            {
                if (!current.gameObject.activeInHierarchy)
                {
                    DebugLogWarning($"Parent {current.name} is inactive!");
                }
                current = current.parent;
            }
        }
    }
    
    /// <summary>
    /// Logs a debug message if debug logs are enabled
    /// </summary>
    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[PauseManager] {message}");
        }
    }
    
    /// <summary>
    /// Logs a warning message if debug logs are enabled
    /// </summary>
    private void DebugLogWarning(string message)
    {
        if (enableDebugLogs)
        {
            Debug.LogWarning($"[PauseManager] {message}");
        }
    }
} 