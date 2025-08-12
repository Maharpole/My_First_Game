using UnityEngine;

public class UIHotkeyToggle : MonoBehaviour
{
    [Header("Hotkey")] public KeyCode key = KeyCode.C;
    [Tooltip("Panels to toggle on/off when the key is pressed")] public GameObject[] panels;
    [Tooltip("If true, ensures only these panels are active and deactivates others in siblings")] public bool exclusive = false;
    [Tooltip("Open on Start")] public bool openOnStart = false;

    void Start()
    {
        if (openOnStart) SetActive(true);
    }

    void Update()
    {
        if (Input.GetKeyDown(key)) Toggle();
    }

    public void Toggle()
    {
        if (panels == null || panels.Length == 0) return;
        bool targetState = !panels[0].activeSelf;
        SetActive(targetState);
    }

    void SetActive(bool state)
    {
        if (panels == null) return;
        foreach (var p in panels)
        {
            if (p != null) p.SetActive(state);
        }
        if (exclusive && state)
        {
            // Optionally deactivate siblings under the same parent
            var any = panels[0];
            if (any != null && any.transform.parent != null)
            {
                foreach (Transform sibling in any.transform.parent)
                {
                    bool isSelected = false;
                    foreach (var p in panels) if (p != null && sibling == p.transform) { isSelected = true; break; }
                    if (!isSelected) sibling.gameObject.SetActive(false);
                }
            }
        }
    }
}

