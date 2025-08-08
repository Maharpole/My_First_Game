using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class EquipmentTestManager : MonoBehaviour
{
    [Header("Test Character")]
    public CharacterEquipment testCharacter;
    
    [Header("Test Equipment")]
    public List<EquipmentData> testItems = new List<EquipmentData>();
    
    [Header("UI References")]
    public Transform buttonContainer;
    public GameObject buttonPrefab;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI statsText;
    
    void Start()
    {
        if (testCharacter == null)
        {
            testCharacter = FindObjectOfType<CharacterEquipment>();
        }
        
        if (testCharacter != null)
        {
            testCharacter.onEquipmentChanged.AddListener(UpdateDisplay);
        }
        
        CreateTestButtons();
        UpdateDisplay();
    }
    
    void CreateTestButtons()
    {
        if (buttonContainer == null || buttonPrefab == null) return;
        
        foreach (var item in testItems)
        {
            if (item == null) continue;
            
            GameObject buttonObj = Instantiate(buttonPrefab, buttonContainer);
            Button button = buttonObj.GetComponent<Button>();
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            
            if (buttonText != null)
            {
                buttonText.text = $"Equip {item.equipmentName}";
            }
            
            if (button != null)
            {
                button.onClick.AddListener(() => EquipItem(item));
            }
        }
        
        // Add unequip buttons for each slot
        var slotTypes = System.Enum.GetValues(typeof(EquipmentType));
        foreach (EquipmentType slotType in slotTypes)
        {
            if (slotType == EquipmentType.None) continue;
            
            GameObject buttonObj = Instantiate(buttonPrefab, buttonContainer);
            Button button = buttonObj.GetComponent<Button>();
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            
            if (buttonText != null)
            {
                buttonText.text = $"Unequip {slotType}";
                buttonText.color = Color.red;
            }
            
            if (button != null)
            {
                button.onClick.AddListener(() => UnequipSlot(slotType));
            }
        }
    }
    
    public void EquipItem(EquipmentData item)
    {
        if (testCharacter != null && item != null)
        {
            bool success = testCharacter.TryEquip(item);
            if (!success)
            {
                Debug.LogWarning($"Failed to equip {item.equipmentName}");
            }
        }
    }
    
    public void UnequipSlot(EquipmentType slotType)
    {
        if (testCharacter != null)
        {
            testCharacter.UnequipSlot(slotType);
        }
    }
    
    void UpdateDisplay()
    {
        if (testCharacter == null) return;
        
        // Update equipment status
        if (statusText != null)
        {
            string status = "=== EQUIPMENT STATUS ===\n";
            status += $"Helmet: {GetSlotStatus(EquipmentType.Helmet)}\n";
            status += $"Amulet: {GetSlotStatus(EquipmentType.Amulet)}\n";
            status += $"Gloves: {GetSlotStatus(EquipmentType.Gloves)}\n";
            status += $"Ring (L): {GetSlotStatus(EquipmentType.RingLeft)}\n";
            status += $"Ring (R): {GetSlotStatus(EquipmentType.RingRight)}\n";
            status += $"Boots: {GetSlotStatus(EquipmentType.Boots)}\n";
            status += $"Belt: {GetSlotStatus(EquipmentType.Belt)}\n";
            status += $"\nTotal: {testCharacter.GetEquippedItemCount()}/7 items equipped";
            
            statusText.text = status;
        }
        
        // Update stats display
        if (statsText != null)
        {
            string stats = "=== TOTAL STATS ===\n";
            var allModifiers = testCharacter.GetAllStatModifiers();
            
            // Group modifiers by stat type
            var groupedStats = new Dictionary<StatType, float>();
            
            foreach (var modifier in allModifiers)
            {
                if (groupedStats.ContainsKey(modifier.statType))
                {
                    groupedStats[modifier.statType] += modifier.value;
                }
                else
                {
                    groupedStats[modifier.statType] = modifier.value;
                }
            }
            
            if (groupedStats.Count == 0)
            {
                stats += "No stat bonuses from equipment";
            }
            else
            {
                foreach (var kvp in groupedStats)
                {
                    stats += $"{kvp.Key}: +{kvp.Value}\n";
                }
            }
            
            statsText.text = stats;
        }
    }
    
    string GetSlotStatus(EquipmentType slotType)
    {
        var slot = testCharacter.GetSlot(slotType);
        return slot.HasItem ? slot.EquippedItem.equipmentName : "Empty";
    }
}
