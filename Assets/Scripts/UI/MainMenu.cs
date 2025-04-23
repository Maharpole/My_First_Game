using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button quitButton;

    private void Start()
    {
        // Set up button listeners
        playButton.onClick.AddListener(PlayGame);
        loadButton.onClick.AddListener(LoadGame);
        quitButton.onClick.AddListener(QuitGame);

        // Disable the load button initially
        loadButton.interactable = false;
    }

    private void PlayGame()
    {
        // Load the game scene (you'll need to create this scene later)
        SceneManager.LoadScene("Forest_Scene");
    }

    private void LoadGame()
    {
        // This will be implemented later
        Debug.Log("Load Game functionality will be implemented later");
    }

    private void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
} 