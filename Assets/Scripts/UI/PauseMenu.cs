using UnityEngine;
using UnityEngine.SceneManagement;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace POEAutoShooter.UI
{
	public class PauseMenu : MonoBehaviour
	{
		[Header("UI References")]
		[SerializeField] private GameObject pauseMenuRoot;

		[Header("Behavior")]
		[SerializeField] private bool pauseOnStart = false;
		[SerializeField] private bool pauseAudioListener = true;
		[SerializeField] private string mainMenuSceneName = ""; // Assign your Main Menu scene name in the inspector

		[Header("Cursor (optional)")]
		[SerializeField] private bool controlCursor = true;
		[SerializeField] private bool pauseCursorVisible = true;
		[SerializeField] private CursorLockMode pauseCursorLockMode = CursorLockMode.None;
		[SerializeField] private bool resumeCursorVisible = true;
		[SerializeField] private CursorLockMode resumeCursorLockMode = CursorLockMode.None;

		private bool isPaused;
		private float previousTimeScale = 1f;

#if ENABLE_INPUT_SYSTEM
		[Header("Input (New Input System - optional)")]
		[SerializeField] private InputActionReference togglePauseAction; // Bind to your Pause action (e.g., Escape)
#endif

		public bool IsPaused => isPaused;

		private void Awake()
		{
			if (pauseMenuRoot != null)
			{
				pauseMenuRoot.SetActive(false);
			}
		}

		private void Start()
		{
			if (pauseOnStart)
			{
				PauseGame();
			}
		}

		private void OnEnable()
		{
#if ENABLE_INPUT_SYSTEM
			if (togglePauseAction != null && togglePauseAction.action != null)
			{
				togglePauseAction.action.performed += OnTogglePausePerformed;
				togglePauseAction.action.Enable();
			}
#endif
		}

		private void OnDisable()
		{
#if ENABLE_INPUT_SYSTEM
			if (togglePauseAction != null && togglePauseAction.action != null)
			{
				togglePauseAction.action.performed -= OnTogglePausePerformed;
				togglePauseAction.action.Disable();
			}
#endif
		}

		private void Update()
		{
			// Legacy input fallback (Escape toggles pause)
			if (Input.GetKeyDown(KeyCode.Escape))
			{
				TogglePause();
			}
		}

		public void TogglePause()
		{
			if (isPaused)
			{
				ResumeGame();
			}
			else
			{
				PauseGame();
			}
		}

		public void PauseGame()
		{
			if (isPaused)
			{
				return;
			}

			isPaused = true;
			previousTimeScale = Time.timeScale;
			Time.timeScale = 0f;

			if (pauseAudioListener)
			{
				AudioListener.pause = true;
			}

			if (pauseMenuRoot != null)
			{
				pauseMenuRoot.SetActive(true);
			}


			if (controlCursor)
			{
				Cursor.visible = pauseCursorVisible;
				Cursor.lockState = pauseCursorLockMode;
			}
		}

		public void ResumeGame()
		{
			if (!isPaused)
			{
				return;
			}

			isPaused = false;
			Time.timeScale = Mathf.Approximately(previousTimeScale, 0f) ? 1f : previousTimeScale;

			if (pauseAudioListener)
			{
				AudioListener.pause = false;
			}

			if (pauseMenuRoot != null)
			{
				pauseMenuRoot.SetActive(false);
			}

			if (controlCursor)
			{
				Cursor.visible = resumeCursorVisible;
				Cursor.lockState = resumeCursorLockMode;
			}
		}

		public void LoadMainMenu()
		{
			// Ensure gameplay is unpaused before scene switch
			Time.timeScale = 1f;
			if (pauseAudioListener)
			{
				AudioListener.pause = false;
			}

			if (!string.IsNullOrWhiteSpace(mainMenuSceneName))
			{
				SceneManager.LoadScene(mainMenuSceneName);
			}
			else
			{
				Debug.LogWarning("PauseMenu: Main Menu scene name is empty. Please assign it in the inspector.");
			}
		}

		public void QuitToDesktop()
		{
			// Ensure unpause for editor consistency
			Time.timeScale = 1f;
			if (pauseAudioListener)
			{
				AudioListener.pause = false;
			}

			Application.Quit();

#if UNITY_EDITOR
			// In the editor, Application.Quit does nothing. Stop play mode for convenience.
			UnityEditor.EditorApplication.isPlaying = false;
#endif
		}

#if ENABLE_INPUT_SYSTEM
		private void OnTogglePausePerformed(InputAction.CallbackContext context)
		{
			if (context.performed)
			{
				TogglePause();
			}
		}
#endif
	}
}


