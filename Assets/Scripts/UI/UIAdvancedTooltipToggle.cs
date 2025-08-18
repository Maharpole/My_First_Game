using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class UIAdvancedTooltipToggle : MonoBehaviour
{
#if ENABLE_INPUT_SYSTEM
    [Header("Input (New Input System)")]
    [Tooltip("Action to hold for extended tooltip view (started=enable, canceled=disable).")]
    public InputActionReference toggleExtendedAction;
    private Input_Control _actions;
#else
    [Header("Input (Legacy)")]
    public KeyCode legacyKey = KeyCode.LeftAlt;
#endif

    void OnEnable()
    {
#if ENABLE_INPUT_SYSTEM
        if (toggleExtendedAction != null)
        {
            toggleExtendedAction.action.started += OnHoldStart;
            toggleExtendedAction.action.canceled += OnHoldEnd;
            toggleExtendedAction.action.Enable();
        }
        else
        {
            _actions = new Input_Control();
            _actions.Enable();
            // Fallback: reuse UI.ToggleInventory; treat started/canceled as hold
            _actions.UI.ToggleInventory.started += OnHoldStart;
            _actions.UI.ToggleInventory.canceled += OnHoldEnd;
        }
#endif
    }

    void OnDisable()
    {
#if ENABLE_INPUT_SYSTEM
        if (toggleExtendedAction != null)
        {
            toggleExtendedAction.action.started -= OnHoldStart;
            toggleExtendedAction.action.canceled -= OnHoldEnd;
            toggleExtendedAction.action.Disable();
        }
        else if (_actions != null)
        {
            _actions.UI.ToggleInventory.started -= OnHoldStart;
            _actions.UI.ToggleInventory.canceled -= OnHoldEnd;
            _actions.Dispose();
            _actions = null;
        }
#endif
    }

#if ENABLE_INPUT_SYSTEM
    void OnHoldStart(InputAction.CallbackContext _) => UITooltip.SetExtendedView(true);
    void OnHoldEnd(InputAction.CallbackContext _) => UITooltip.SetExtendedView(false);
#else
    void Update()
    {
        UITooltip.SetExtendedView(Input.GetKey(legacyKey));
    }
#endif
}


