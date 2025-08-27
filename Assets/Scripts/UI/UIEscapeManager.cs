using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// Handles Escape key: closes all open UI windows; if none are open, toggles pause.
public class UIEscapeManager : MonoBehaviour
{
    [Tooltip("Assign windows (root GameObjects) that should close on Escape")] public GameObject[] windows;
    [Tooltip("GameObject to toggle for Pause menu if no other windows are open")] public GameObject pauseMenu;

    void Update()
    {
        if (EscapePressed())
        {
            if (CloseAnyOpenWindow()) return;
            TogglePause();
        }
    }

    bool EscapePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    bool CloseAnyOpenWindow()
    {
        bool closed = false;
        if (windows == null) return false;
        for (int i = 0; i < windows.Length; i++)
        {
            var go = windows[i];
            if (go != null && go.activeInHierarchy)
            {
                go.SetActive(false);
                closed = true;
            }
        }
        return closed;
    }

    void TogglePause()
    {
        if (pauseMenu == null) return;
        bool show = !pauseMenu.activeSelf;
        pauseMenu.SetActive(show);
        Time.timeScale = show ? 0f : 1f;
    }
}


