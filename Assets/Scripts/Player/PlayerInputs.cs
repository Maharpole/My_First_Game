// Only compile full helper when the new Input System package is available.
#if ENABLE_INPUT_SYSTEM
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-210)]
public class PlayerInputs : MonoBehaviour
{
    [Header("Gameplay")]
    public InputAction Move;
    public InputAction Dash;

    private void OnEnable()
    {
        if (Move == null)
        {
            Move = new InputAction("Move", InputActionType.Value);
            Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
        }
        if (Dash == null)
        {
            Dash = new InputAction("Dash", InputActionType.Button, "<Keyboard>/space");
        }
        Move.Enable();
        Dash.Enable();
    }

    private void OnDisable()
    {
        Move.Disable();
        Dash.Disable();
    }
}
#else
using UnityEngine;

// Minimal stub so projects without the new Input System still compile
public class PlayerInputs : MonoBehaviour { }
#endif // ENABLE_INPUT_SYSTEM
