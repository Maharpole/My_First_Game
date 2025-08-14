using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIInventorySlotSelector : MonoBehaviour, IPointerClickHandler
{
    public SimpleInventory inventory;
    public int slotIndex;
    public Image highlightFrame; // optional: assign a child Image used as selection frame

    public static EquipmentData SelectedItem { get; private set; }
    public static System.Action<EquipmentData> onSelectionChanged;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (inventory == null || slotIndex < 0 || slotIndex >= inventory.Capacity) return;
        SelectedItem = inventory.Get(slotIndex);
        onSelectionChanged?.Invoke(SelectedItem);
        UpdateHighlight(true);
        // Clear highlight on other selectors
        var others = Object.FindObjectsByType<UIInventorySlotSelector>(FindObjectsSortMode.None);
        for (int i = 0; i < others.Length; i++)
        {
            if (others[i] != this) others[i].UpdateHighlight(false);
        }
    }

    void OnEnable()
    {
        UpdateHighlight(false);
    }

    void OnDisable()
    {
        UpdateHighlight(false);
    }

    public void UpdateHighlight(bool selected)
    {
        if (highlightFrame != null)
        {
            highlightFrame.enabled = selected;
        }
    }
}



