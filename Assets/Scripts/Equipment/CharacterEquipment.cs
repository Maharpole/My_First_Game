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
    public EquipmentSlot mainHand = new EquipmentSlot(EquipmentType.MainHand, "Main Hand");
    public EquipmentSlot offHand = new EquipmentSlot(EquipmentType.OffHand, "Off Hand");

    [Header("Events")]
    public UnityEvent onEquipmentChanged;

    private Dictionary<EquipmentType, EquipmentSlot> slotMap;

    void Awake()
    {
        InitializeSlots();
    }

    void InitializeSlots()
    {
        slotMap = new Dictionary<EquipmentType, EquipmentSlot>
        {
            { EquipmentType.Helmet, helmet },
            { EquipmentType.Amulet, amulet },
            { EquipmentType.Gloves, gloves },
            { EquipmentType.RingLeft, ringLeft },
            { EquipmentType.RingRight, ringRight },
            { EquipmentType.Boots, boots },
            { EquipmentType.Belt, belt },
            { EquipmentType.MainHand, mainHand },
            { EquipmentType.OffHand, offHand }
        };

        foreach (var slot in slotMap.Values)
        {
            slot.onItemEquipped.AddListener(OnItemEquipped);
            slot.onItemUnequipped.AddListener(OnItemUnequipped);
        }
    }

    public bool TryEquip(EquipmentData item)
    {
        if (item == null) return false;

        // Rings auto-fit left then right
        if (item.equipmentType == EquipmentType.RingLeft || item.equipmentType == EquipmentType.RingRight)
        {
            if (ringLeft.IsEmpty && ringLeft.TryEquip(item)) return true;
            if (ringRight.IsEmpty && ringRight.TryEquip(item)) return true;
            return ringLeft.TryEquip(item);
        }

        // Hand logic
        if (item.isWeapon || item.equipmentType == EquipmentType.OffHand)
        {
            return TryEquipHand(item);
        }

        // Other explicit slots
        if (slotMap.TryGetValue(item.equipmentType, out EquipmentSlot slot))
        {
            return slot.TryEquip(item);
        }

        Debug.LogWarning($"No slot found for equipment type: {item.equipmentType}");
        return false;
    }

    bool TryEquipHand(EquipmentData item)
    {
        // Two-hand weapon: place in main hand, clear offhand
        if (item.isWeapon && (item.handUsage == HandUsage.TwoHand || item.occupiesBothHands))
        {
            // Unequip offhand if present
            if (offHand.HasItem) offHand.Unequip();
            return mainHand.TryEquip(item);
        }

        // One-hand weapon vs offhand
        if (item.isWeapon)
        {
            // Prefer main hand; if occupied by two-hand, clear it
            if (mainHand.HasItem && mainHand.EquippedItem != null && (mainHand.EquippedItem.occupiesBothHands || mainHand.EquippedItem.handUsage == HandUsage.TwoHand))
            {
                mainHand.Unequip();
            }
            return mainHand.TryEquip(item);
        }
        else // item is an offhand (shield/quiver/focus)
        {
            // Cannot equip offhand if main hand has a two-hand weapon
            if (mainHand.HasItem && mainHand.EquippedItem != null && (mainHand.EquippedItem.occupiesBothHands || mainHand.EquippedItem.handUsage == HandUsage.TwoHand))
            {
                Debug.LogWarning("Cannot equip offhand with a two-hand weapon equipped.");
                return false;
            }
            return offHand.TryEquip(item);
        }
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
        return slotMap.Values.Where(s => s.HasItem).Select(s => s.EquippedItem).ToList();
    }

    public List<StatModifier> GetAllStatModifiers()
    {
        var all = new List<StatModifier>();
        foreach (var item in GetAllEquippedItems()) all.AddRange(item.AllStats);
        return all;
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
        onEquipmentChanged?.Invoke();
    }

    private void OnItemUnequipped(EquipmentData item)
    {
        onEquipmentChanged?.Invoke();
    }

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
        Debug.Log($"Total items equipped: {GetEquippedItemCount()}");
    }
}
