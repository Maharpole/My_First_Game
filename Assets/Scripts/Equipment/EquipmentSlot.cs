using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class EquipmentSlot
{
    [Header("Slot Configuration")]
    public EquipmentType slotType;
    public string slotName;
    
    [Header("Current Equipment")]
    [SerializeField] private EquipmentData equippedItem;
    
    [Header("Events")]
    public UnityEvent<EquipmentData> onItemEquipped;
    public UnityEvent<EquipmentData> onItemUnequipped;
    
    // Properties
    public EquipmentData EquippedItem => equippedItem;
    public bool IsEmpty => equippedItem == null;
    public bool HasItem => equippedItem != null;
    
    public EquipmentSlot(EquipmentType type, string name)
    {
        slotType = type;
        slotName = name;
        onItemEquipped = new UnityEvent<EquipmentData>();
        onItemUnequipped = new UnityEvent<EquipmentData>();
    }
    
    public bool CanEquip(EquipmentData item)
    {
        if (item == null) return false;
        return item.CanEquipInSlot(slotType);
    }
    
    public bool TryEquip(EquipmentData item)
    {
        if (!CanEquip(item)) 
        {
            Debug.LogWarning($"Cannot equip {item.equipmentName} in {slotName} slot. Wrong equipment type.");
            return false;
        }
        
        // Unequip current item if exists
        if (HasItem)
        {
            Unequip();
        }
        
        // Equip new item
        equippedItem = item;
        onItemEquipped?.Invoke(item);
        
        Debug.Log($"Equipped {item.equipmentName} in {slotName} slot");
        return true;
    }
    
    public EquipmentData Unequip()
    {
        if (IsEmpty) return null;
        
        EquipmentData unequippedItem = equippedItem;
        equippedItem = null;
        
        onItemUnequipped?.Invoke(unequippedItem);
        Debug.Log($"Unequipped {unequippedItem.equipmentName} from {slotName} slot");
        
        return unequippedItem;
    }
    
    public void ForceSetItem(EquipmentData item)
    {
        equippedItem = item;
    }
}
