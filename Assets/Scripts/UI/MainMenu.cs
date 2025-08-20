using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button quitButton;

    private void Start()
    {
        // Set up button listeners
        playButton.onClick.AddListener(PlayGame);
        if (newGameButton != null)
        {
            newGameButton.onClick.AddListener(NewGame);
        }
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

    private void NewGame()
    {
        // Go to character creation scene where the player chooses a starting class
        SceneManager.LoadScene("CharacterCreation");
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