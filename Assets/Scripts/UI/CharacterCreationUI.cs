using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public enum StartingClass
{
    Strength,
    Dexterity,
    Intelligence
}

public class CharacterCreationUI : MonoBehaviour
{
    [Header("Buttons")] public Button strengthButton;
    public Button dexterityButton;
    public Button intelligenceButton;
    public Button confirmButton;
    public Button backButton;

    [Header("Preview/State")] public Text selectedLabel;
    [Header("Name Input")] public TMP_InputField nameInputTMP; public InputField nameInput;

    [Header("Selection Colors")] public Color unselectedColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    public Color selectedColor = new Color(0.25f, 0.6f, 1f, 1f);

    private StartingClass? _pendingSelection;

    void Start()
    {
        if (strengthButton != null) strengthButton.onClick.AddListener(() => Select(StartingClass.Strength));
        if (dexterityButton != null) dexterityButton.onClick.AddListener(() => Select(StartingClass.Dexterity));
        if (intelligenceButton != null) intelligenceButton.onClick.AddListener(() => Select(StartingClass.Intelligence));
        if (confirmButton != null) confirmButton.onClick.AddListener(Confirm);
        if (backButton != null) backButton.onClick.AddListener(BackToMenu);

        UpdateLabel();
        UpdateSelectionVisuals();
    }

    void Select(StartingClass chosen)
    {
        _pendingSelection = chosen;
        UpdateSelectionVisuals();
    }

    void UpdateLabel()
    {
        if (selectedLabel == null) return;
        selectedLabel.text = _pendingSelection.HasValue ? $"Selected: {_pendingSelection.Value}" : "Select your class";
    }

    void UpdateSelectionVisuals()
    {
        // Determine target colors
        ApplyButtonColor(strengthButton, _pendingSelection.HasValue && _pendingSelection.Value == StartingClass.Strength ? selectedColor : unselectedColor);
        ApplyButtonColor(dexterityButton, _pendingSelection.HasValue && _pendingSelection.Value == StartingClass.Dexterity ? selectedColor : unselectedColor);
        ApplyButtonColor(intelligenceButton, _pendingSelection.HasValue && _pendingSelection.Value == StartingClass.Intelligence ? selectedColor : unselectedColor);
    }

    void ApplyButtonColor(Button btn, Color color)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = color;
        // Also update ColorBlock normal color so hover/pressed blend from this
        var cb = btn.colors;
        cb.normalColor = color;
        btn.colors = cb;
    }

    void Confirm()
    {
        if (!_pendingSelection.HasValue) return;
        // Gather name from TMP or legacy InputField
        string enteredName = null;
        if (nameInputTMP != null) enteredName = nameInputTMP.text;
        if (string.IsNullOrWhiteSpace(enteredName) && nameInput != null) enteredName = nameInput.text;
        if (string.IsNullOrWhiteSpace(enteredName)) enteredName = "Hero";
        // Persist selection
        PlayerProfile.NewGame(_pendingSelection.Value, enteredName);
        // Load first gameplay scene
        SceneManager.LoadScene("Map_Scene");
    }

    void BackToMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}


