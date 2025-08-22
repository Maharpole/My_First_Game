using UnityEngine;
using UnityEngine.InputSystem;

public class UIAdvancedTooltipToggle : MonoBehaviour
{
    [Header("Input (New Input System)")]
    [Tooltip("Action to hold for extended tooltip view (started=enable, canceled=disable).")]
    public InputActionReference toggleExtendedAction;
    private Input_Control _actions;

    void OnEnable()
    {
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
    }

    void OnDisable()
    {
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
    }

    void OnHoldStart(InputAction.CallbackContext _) => UITooltip.SetExtendedView(true);
    void OnHoldEnd(InputAction.CallbackContext _) => UITooltip.SetExtendedView(false);
}


