using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class InventoryUIController : MonoBehaviour
{
    [Header("References")]
    public GameObject rootPanel; // parent panel to show/hide
    public Transform inventoryListContainer;
    public GameObject inventoryItemButtonPrefab;

    public TextMeshProUGUI mainHandText;
    public TextMeshProUGUI offHandText;
    public TextMeshProUGUI helmetText;
    public TextMeshProUGUI amuletText;
    public TextMeshProUGUI glovesText;
    public TextMeshProUGUI ringLText;
    public TextMeshProUGUI ringRText;
    public TextMeshProUGUI bootsText;
    public TextMeshProUGUI beltText;

    private InventorySystem inventory;
    private CharacterEquipment equipment;

    void Start()
    {
        if (rootPanel != null) rootPanel.SetActive(false);
        inventory = FindObjectOfType<InventorySystem>();
        equipment = FindObjectOfType<CharacterEquipment>();
        if (inventory != null) inventory.onChanged.AddListener(Refresh);
        Refresh();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            Toggle();
        }
    }

    public void Toggle()
    {
        if (rootPanel == null) return;
        bool active = !rootPanel.activeSelf;
        rootPanel.SetActive(active);
        if (active) Refresh();
    }

    void Refresh()
    {
        RefreshInventoryList();
        RefreshEquipmentTexts();
    }

    void RefreshInventoryList()
    {
        if (inventoryListContainer == null || inventoryItemButtonPrefab == null || inventory == null) return;
        foreach (Transform child in inventoryListContainer) Destroy(child.gameObject);
        for (int i = 0; i < inventory.items.Count; i++)
        {
            var item = inventory.items[i];
            var go = Instantiate(inventoryItemButtonPrefab, inventoryListContainer);
            var label = go.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = item != null ? item.equipmentName : "(null)";
            var btn = go.GetComponent<Button>();
            int idx = i;
            if (btn != null)
            {
                btn.onClick.AddListener(() => OnClickInventoryItem(idx));
            }
        }
    }

    void RefreshEquipmentTexts()
    {
        if (equipment == null) return;
        SetSlotText(mainHandText, equipment.mainHand);
        SetSlotText(offHandText, equipment.offHand);
        SetSlotText(helmetText, equipment.helmet);
        SetSlotText(amuletText, equipment.amulet);
        SetSlotText(glovesText, equipment.gloves);
        SetSlotText(ringLText, equipment.ringLeft);
        SetSlotText(ringRText, equipment.ringRight);
        SetSlotText(bootsText, equipment.boots);
        SetSlotText(beltText, equipment.belt);
    }

    void SetSlotText(TextMeshProUGUI text, EquipmentSlot slot)
    {
        if (text == null || slot == null) return;
        text.text = slot.HasItem ? slot.EquippedItem.equipmentName : "Empty";
    }

    void OnClickInventoryItem(int index)
    {
        if (inventory == null || equipment == null) return;
        if (index < 0 || index >= inventory.items.Count) return;
        var item = inventory.items[index];
        if (item == null) return;

        // Try equip appropriate slot
        if (equipment.TryEquip(item))
        {
            // remove from inventory on success
            inventory.Remove(item);
            Refresh();
        }
    }

    // Optional: buttons in UI to unequip to inventory
    public void UnequipToInventory(EquipmentType slot)
    {
        if (equipment == null || inventory == null) return;
        var removed = equipment.UnequipSlot(slot);
        if (removed != null)
        {
            inventory.TryAdd(removed);
            Refresh();
        }
    }
}
