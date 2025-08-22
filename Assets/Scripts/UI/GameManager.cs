using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The game over panel that appears when the player dies")]
    public GameObject gameOverPanel;
    
    [Tooltip("Text that shows the final score or message")]
    public TextMeshProUGUI gameOverText;
    
    [Tooltip("Button to restart the game")]
    public Button restartButton;
    
    [Tooltip("Button to quit the game")]
    public Button quitButton;
    
    [Header("Game Over Settings")]
    [Tooltip("Time to wait before showing the game over screen")]
    public float gameOverDelay = 1.5f;
    
    [Tooltip("Message to show when the game is over")]
    public string gameOverMessage = "Game Over!";
    
    private bool isGameOver = false;
    
    void Start()
    {
        // Hide the game over panel at start
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        
        // Set up button listeners
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartGame);
        }
        
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitGame);
        }
        
        // Weapon system is under refactor; starting weapon assignment removed.
    }
    
    public void GameOver()
    {
        if (!isGameOver)
        {
            isGameOver = true;
            StartCoroutine(ShowGameOverScreen());
        }
    }
    
    IEnumerator ShowGameOverScreen()
    {
        // Wait for the delay
        yield return new WaitForSeconds(gameOverDelay);
        
        // Show the game over panel
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        
        // Update the game over text
        if (gameOverText != null)
        {
            gameOverText.text = gameOverMessage;
        }
        
        // Pause the game
        Time.timeScale = 0f;
    }
    
    public void RestartGame()
    {
        // Resume time
        Time.timeScale = 1f;
        
        // Reload the current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
} 