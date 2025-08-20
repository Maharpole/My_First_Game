using UnityEngine;
using UnityEngine.UI;

// Simple helper to wire a UI Button to SaveSystem.SaveNow
public class SavePanelHook : MonoBehaviour
{
    public Button saveButton;

    void Awake()
    {
        if (saveButton == null) saveButton = GetComponentInChildren<Button>(true);
        if (saveButton != null) saveButton.onClick.AddListener(OnSaveClicked);
    }

    void OnDestroy()
    {
        if (saveButton != null) saveButton.onClick.RemoveListener(OnSaveClicked);
    }

    void OnSaveClicked()
    {
        SaveSystem.SaveNow();
    }
}


