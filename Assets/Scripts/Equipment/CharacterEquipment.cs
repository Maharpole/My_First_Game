using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Linq;

public class CharacterEquipment : MonoBehaviour
{
    [Header("Equipment Slots")]
    public EquipmentSlot helmet = new EquipmentSlot(EquipmentType.Helmet, "Helmet");
    public EquipmentSlot amulet = new EquipmentSlot(EquipmentType.Amulet, "Amulet");
    public EquipmentSlot gloves = new EquipmentSlot(EquipmentType.Gloves, "Gloves");
    public EquipmentSlot ringLeft = new EquipmentSlot(EquipmentType.RingLeft, "Ring (L)");
    public EquipmentSlot ringRight = new EquipmentSlot(EquipmentType.RingRight, "Ring (R)");
    public EquipmentSlot boots = new EquipmentSlot(EquipmentType.Boots, "Boots");
    public EquipmentSlot belt = new EquipmentSlot(EquipmentType.Belt, "Belt");
    
    [Header("Events")]
    public UnityEvent onEquipmentChanged;
    
    private Dictionary<EquipmentType, EquipmentSlot> slotMap;
    
    void Awake()
    {
        InitializeSlots();
    }
    
    void InitializeSlots()
    {
        // Create slot mapping for easy access
        slotMap = new Dictionary<EquipmentType, EquipmentSlot>
        {
            { EquipmentType.Helmet, helmet },
            { EquipmentType.Amulet, amulet },
            { EquipmentType.Gloves, gloves },
            { EquipmentType.RingLeft, ringLeft },
            { EquipmentType.RingRight, ringRight },
            { EquipmentType.Boots, boots },
            { EquipmentType.Belt, belt }
        };
        
        // Subscribe to equipment change events
        foreach (var slot in slotMap.Values)
        {
            slot.onItemEquipped.AddListener(OnItemEquipped);
            slot.onItemUnequipped.AddListener(OnItemUnequipped);
        }
    }
    
    public bool TryEquip(EquipmentData item)
    {
        if (item == null) return false;
        
        // For rings, try left first, then right
        if (item.equipmentType == EquipmentType.RingLeft || item.equipmentType == EquipmentType.RingRight)
        {
            // Try left ring first
            if (ringLeft.IsEmpty && ringLeft.TryEquip(item))
            {
                return true;
            }
            // Then try right ring
            if (ringRight.IsEmpty && ringRight.TryEquip(item))
            {
                return true;
            }
            // If both occupied, replace left ring
            return ringLeft.TryEquip(item);
        }
        
        // For other equipment types, use the specific slot
        if (slotMap.TryGetValue(item.equipmentType, out EquipmentSlot slot))
        {
            return slot.TryEquip(item);
        }
        
        Debug.LogWarning($"No slot found for equipment type: {item.equipmentType}");
        return false;
    }
    
    public EquipmentData UnequipSlot(EquipmentType slotType)
    {
        if (slotMap.TryGetValue(slotType, out EquipmentSlot slot))
        {
            return slot.Unequip();
        }
        return null;
    }
    
    public EquipmentSlot GetSlot(EquipmentType slotType)
    {
        slotMap.TryGetValue(slotType, out EquipmentSlot slot);
        return slot;
    }
    
    public List<EquipmentData> GetAllEquippedItems()
    {
        return slotMap.Values
            .Where(slot => slot.HasItem)
            .Select(slot => slot.EquippedItem)
            .ToList();
    }
    
    public List<StatModifier> GetAllStatModifiers()
    {
        var allModifiers = new List<StatModifier>();
        
        foreach (var item in GetAllEquippedItems())
        {
            allModifiers.AddRange(item.AllStats);
        }
        
        return allModifiers;
    }
    
    public bool HasItemEquipped(EquipmentData item)
    {
        return GetAllEquippedItems().Contains(item);
    }
    
    public int GetEquippedItemCount()
    {
        return GetAllEquippedItems().Count;
    }
    
    private void OnItemEquipped(EquipmentData item)
    {
        Debug.Log($"Equipment changed: {item.equipmentName} equipped");
        onEquipmentChanged?.Invoke();
    }
    
    private void OnItemUnequipped(EquipmentData item)
    {
        Debug.Log($"Equipment changed: {item.equipmentName} unequipped");
        onEquipmentChanged?.Invoke();
    }
    
    // Utility method for debugging
    [ContextMenu("Print Equipment Status")]
    public void PrintEquipmentStatus()
    {
        Debug.Log("=== EQUIPMENT STATUS ===");
        foreach (var kvp in slotMap)
        {
            var slot = kvp.Value;
            string status = slot.HasItem ? slot.EquippedItem.equipmentName : "Empty";
            Debug.Log($"{slot.slotName}: {status}");
        }
        Debug.Log($"Total items equipped: {GetEquippedItemCount()}/7");
    }
}
