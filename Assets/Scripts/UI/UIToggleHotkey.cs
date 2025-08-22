using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class UIToggleHotkey : MonoBehaviour
{
    [Header("Input (New Input System)")]
    [Tooltip("Action that triggers the toggle (performed callback). Bind this in your .inputactions (e.g., key C).")]
    public InputActionReference toggleAction;
    private Input_Control _actions; // fallback if toggleAction is not wired in the scene

    [Header("Targets")]
    [Tooltip("Panels to toggle on/off when the action is triggered")] public GameObject[] panels;
    [Tooltip("If true, hide all siblings under the same parent when showing these panels")] public bool exclusive = false;
    [Tooltip("Open panels on Start")] public bool openOnStart = false;
    [Tooltip("Ignore hotkey when a TMP/InputField is focused")] public bool ignoreWhenTextFieldFocused = true;
    [Tooltip("If true, still allow closing panels even if a text field is focused")] public bool allowCloseWhenTextFieldFocused = true;
    [Tooltip("If true, still allow opening panels even if a text field is focused")] public bool allowOpenWhenTextFieldFocused = true;
    [Header("Pause Control")]
    [Tooltip("Pause the game (Time.timeScale) while these panels are open")] public bool pauseWhenOpen = false;
    [Tooltip("Time scale to use when paused (0 = fully paused)")] public float pausedTimeScale = 0f;
    float _previousTimeScale = 1f;
    [Header("Optional Save Button")]
    [Tooltip("If true, when toggling this panel ON, will show a Save button if a SavePanelHook is present")] public bool enableSaveButton = false;

    void OnEnable()
    {
        if (toggleAction != null)
        {
            toggleAction.action.performed += OnToggle;
            toggleAction.action.Enable();
        }
        else
        {
            // Fallback: auto-bind to generated Input_Control UI.ToggleInventory so hotkey works without manual wiring
            _actions = new Input_Control();
            _actions.Enable();
            _actions.UI.ToggleInventory.performed += OnToggle;
        }
        if (openOnStart) SetActive(true);
        if (panels != null && panels.Length > 0 && panels[0] != null)
        {
            Debug.Log($"[UIToggleHotkey] Enabled. Panels[0]={panels[0].name}. Action={(toggleAction!=null ? toggleAction.action.name : "(generated)")}");
        }
    }

    void OnDisable()
    {
        if (toggleAction != null)
        {
            toggleAction.action.performed -= OnToggle;
            toggleAction.action.Disable();
        }
        else if (_actions != null)
        {
            _actions.UI.ToggleInventory.performed -= OnToggle;
            _actions.Dispose();
            _actions = null;
        }
    }

    void OnToggle(InputAction.CallbackContext _)
    {
        if (ShouldIgnore())
        {
            bool anyActive = AnyPanelActive();
            bool wantOpen = !anyActive;
            bool wantClose = anyActive;
            if (!((wantClose && allowCloseWhenTextFieldFocused) || (wantOpen && allowOpenWhenTextFieldFocused))) return;
        }
        Toggle();
    }

    bool ShouldIgnore()
    {
        if (!ignoreWhenTextFieldFocused) return false;
        if (EventSystem.current == null) return false;
        var go = EventSystem.current.currentSelectedGameObject;
        if (go == null) return false;
        // Ignore only when a text input field is focused
        return go.GetComponent<TMP_InputField>() != null || go.GetComponent<InputField>() != null;
    }

    bool AnyPanelActive()
    {
        if (panels == null) return false;
        for (int i = 0; i < panels.Length; i++)
        {
            var p = panels[i];
            if (p != null && p.activeInHierarchy) return true;
        }
        return false;
    }

    public void Toggle()
    {
        if (panels == null || panels.Length == 0) return;
        bool target = !panels[0].activeSelf;
        SetActive(target);
    }

    void SetActive(bool state)
    {
        if (panels == null) return;
        foreach (var p in panels)
        {
            if (p != null) p.SetActive(state);
        }
        if (pauseWhenOpen)
        {
            if (state)
            {
                _previousTimeScale = Time.timeScale;
                Time.timeScale = Mathf.Clamp(pausedTimeScale, 0f, 1f);
            }
            else
            {
                Time.timeScale = _previousTimeScale <= 0f ? 1f : _previousTimeScale;
            }
        }
        if (!state)
        {
            // Hide any global tooltip when UI panels are closed
            UITooltip.Hide();
        }
        if (exclusive && state && panels.Length > 0 && panels[0] != null && panels[0].transform.parent != null)
        {
            var parent = panels[0].transform.parent;
            foreach (Transform sib in parent)
            {
                bool isTarget = false;
                for (int i = 0; i < panels.Length; i++)
                {
                    if (panels[i] != null && sib.gameObject == panels[i]) { isTarget = true; break; }
                }
                if (!isTarget) sib.gameObject.SetActive(false);
            }
        }
    }
}
