using UnityEngine;
using UnityEngine.UI;

public class PauseMenuUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Reference to the resume button")]
    public Button resumeButton;
    
    [Tooltip("Reference to the main menu button")]
    public Button mainMenuButton;
    
    [Tooltip("Reference to the quit button")]
    public Button quitButton;
    
    private void Start()
    {
        // Set up button listeners
        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(OnResumeClicked);
        }
        
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }
        
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitClicked);
        }
        
        // Hide the menu initially
        gameObject.SetActive(false);
    }
    
    private void OnResumeClicked()
    {
        if (PauseManager.Instance != null)
        {
            PauseManager.Instance.ResumeGame();
        }
    }
    
    private void OnMainMenuClicked()
    {
        if (PauseManager.Instance != null)
        {
            PauseManager.Instance.ReturnToMainMenu();
        }
    }
    
    private void OnQuitClicked()
    {
        if (PauseManager.Instance != null)
        {
            PauseManager.Instance.QuitGame();
        }
    }
} 