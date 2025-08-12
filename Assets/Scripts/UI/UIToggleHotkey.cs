using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem; // New Input System
#endif

public class UIToggleHotkey : MonoBehaviour
{
#if ENABLE_INPUT_SYSTEM
    [Header("Input (New Input System)")]
    [Tooltip("Action that triggers the toggle (performed callback). Bind this in your .inputactions (e.g., key C).")]
    public InputActionReference toggleAction;
#else
    [Header("Input (Legacy)")]
    [Tooltip("Legacy fallback hotkey when New Input System is not enabled")] public KeyCode legacyKey = KeyCode.C;
#endif

    [Header("Targets")]
    [Tooltip("Panels to toggle on/off when the action is triggered")] public GameObject[] panels;
    [Tooltip("If true, hide all siblings under the same parent when showing these panels")] public bool exclusive = false;
    [Tooltip("Open panels on Start")] public bool openOnStart = false;
    [Tooltip("Ignore hotkey when a TMP/InputField is focused")] public bool ignoreWhenTextFieldFocused = true;

    void OnEnable()
    {
#if ENABLE_INPUT_SYSTEM
        if (toggleAction != null)
        {
            toggleAction.action.performed += OnToggle;
            toggleAction.action.Enable();
        }
#endif
        if (openOnStart) SetActive(true);
    }

    void OnDisable()
    {
#if ENABLE_INPUT_SYSTEM
        if (toggleAction != null)
        {
            toggleAction.action.performed -= OnToggle;
            toggleAction.action.Disable();
        }
#endif
    }

#if ENABLE_INPUT_SYSTEM
    void OnToggle(UnityEngine.InputSystem.InputAction.CallbackContext _)
    {
        if (ShouldIgnore()) return;
        Toggle();
    }
#else
    void Update()
    {
        if (Input.GetKeyDown(legacyKey))
        {
            if (ShouldIgnore()) return;
            Toggle();
        }
    }
#endif

    bool ShouldIgnore()
    {
        if (!ignoreWhenTextFieldFocused) return false;
        if (EventSystem.current == null) return false;
        var go = EventSystem.current.currentSelectedGameObject;
        if (go == null) return false;
        // Ignore only when a text input field is focused
        return go.GetComponent<TMP_InputField>() != null || go.GetComponent<InputField>() != null;
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
