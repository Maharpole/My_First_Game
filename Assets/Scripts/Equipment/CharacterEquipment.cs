using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Linq;

public class CharacterEquipment : MonoBehaviour
{
    [Header("Equipment Slots")]
    public EquipmentSlot helmet = new EquipmentSlot(EquipmentType.Helmet, "Helmet");
    public EquipmentSlot bodyArmour = new EquipmentSlot(EquipmentType.BodyArmour, "Body Armour");
    public EquipmentSlot amulet = new EquipmentSlot(EquipmentType.Amulet, "Amulet");
    public EquipmentSlot gloves = new EquipmentSlot(EquipmentType.Gloves, "Gloves");
    public EquipmentSlot ring1 = new EquipmentSlot(EquipmentType.Ring, "Ring 1");
    public EquipmentSlot ring2 = new EquipmentSlot(EquipmentType.Ring, "Ring 2");
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
        // Ensure slot identities are correct even if serialized data is stale
        if (helmet == null) helmet = new EquipmentSlot(EquipmentType.Helmet, "Helmet"); else { helmet.slotType = EquipmentType.Helmet; helmet.slotName = "Helmet"; }
        if (bodyArmour == null) bodyArmour = new EquipmentSlot(EquipmentType.BodyArmour, "Body Armour"); else { bodyArmour.slotType = EquipmentType.BodyArmour; bodyArmour.slotName = "Body Armour"; }
        if (amulet == null) amulet = new EquipmentSlot(EquipmentType.Amulet, "Amulet"); else { amulet.slotType = EquipmentType.Amulet; amulet.slotName = "Amulet"; }
        if (gloves == null) gloves = new EquipmentSlot(EquipmentType.Gloves, "Gloves"); else { gloves.slotType = EquipmentType.Gloves; gloves.slotName = "Gloves"; }
        if (ring1 == null) ring1 = new EquipmentSlot(EquipmentType.Ring, "Ring 1"); else { ring1.slotType = EquipmentType.Ring; ring1.slotName = "Ring 1"; }
        if (ring2 == null) ring2 = new EquipmentSlot(EquipmentType.Ring, "Ring 2"); else { ring2.slotType = EquipmentType.Ring; ring2.slotName = "Ring 2"; }
        if (boots == null) boots = new EquipmentSlot(EquipmentType.Boots, "Boots"); else { boots.slotType = EquipmentType.Boots; boots.slotName = "Boots"; }
        if (belt == null) belt = new EquipmentSlot(EquipmentType.Belt, "Belt"); else { belt.slotType = EquipmentType.Belt; belt.slotName = "Belt"; }
        if (mainHand == null) mainHand = new EquipmentSlot(EquipmentType.MainHand, "Main Hand"); else { mainHand.slotType = EquipmentType.MainHand; mainHand.slotName = "Main Hand"; }
        if (offHand == null) offHand = new EquipmentSlot(EquipmentType.OffHand, "Off Hand"); else { offHand.slotType = EquipmentType.OffHand; offHand.slotName = "Off Hand"; }

        slotMap = new Dictionary<EquipmentType, EquipmentSlot>
        {
            { EquipmentType.Helmet, helmet },
            { EquipmentType.BodyArmour, bodyArmour },
            { EquipmentType.Amulet, amulet },
            { EquipmentType.Gloves, gloves },
            { EquipmentType.Ring, ring1 }, // default accessor maps to ring1
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

        if (item.equipmentType == EquipmentType.Ring)
        {
            if (ring1.IsEmpty && ring1.TryEquip(item)) return true;
            if (ring2.IsEmpty && ring2.TryEquip(item)) return true;
            return ring1.TryEquip(item);
        }

        if (item.isWeapon || item.equipmentType == EquipmentType.OffHand)
        {
            return TryEquipHand(item);
        }

        var slot = GetSlot(item.equipmentType);
        if (slot != null)
        {
            return slot.TryEquip(item);
        }

        Debug.LogWarning($"No slot found for equipment type: {item.equipmentType}");
        return false;
    }

    // Used by inventory click-to-equip so caller can put swapped item back into inventory
    public bool TryEquipFromInventory(EquipmentData item, out EquipmentData swapped)
    {
        swapped = null;
        if (item == null) return false;

        if (item.equipmentType == EquipmentType.Ring)
        {
            if (ring1.IsEmpty) return ring1.TryEquip(item);
            if (ring2.IsEmpty) return ring2.TryEquip(item);
            return ring1.TryEquipWithSwap(item, out swapped);
        }

        if (item.isWeapon || item.equipmentType == EquipmentType.OffHand)
        {
            // For now, perform standard hand logic; swapped main-hand item (if any) is returned
            if (item.isWeapon && (item.handUsage == HandUsage.TwoHand || item.occupiesBothHands))
            {
                if (mainHand.HasItem) swapped = mainHand.Unequip();
                if (offHand.HasItem) offHand.Unequip();
                return mainHand.TryEquip(item);
            }

            if (item.isWeapon)
            {
                if (mainHand.HasItem && mainHand.EquippedItem != null && (mainHand.EquippedItem.occupiesBothHands || mainHand.EquippedItem.handUsage == HandUsage.TwoHand))
                {
                    swapped = mainHand.Unequip();
                }
                else if (mainHand.HasItem)
                {
                    swapped = mainHand.Unequip();
                }
                return mainHand.TryEquip(item);
            }
            else
            {
                if (mainHand.HasItem && mainHand.EquippedItem != null && (mainHand.EquippedItem.occupiesBothHands || mainHand.EquippedItem.handUsage == HandUsage.TwoHand))
                {
                    // cannot equip offhand; no change
                    return false;
                }
                if (offHand.HasItem) swapped = offHand.Unequip();
                return offHand.TryEquip(item);
            }
        }

        var slot = GetSlot(item.equipmentType);
        if (slot != null)
        {
            return slot.TryEquipWithSwap(item, out swapped);
        }
        return false;
    }

    bool TryEquipHand(EquipmentData item)
    {
        if (item.isWeapon && (item.handUsage == HandUsage.TwoHand || item.occupiesBothHands))
        {
            if (offHand.HasItem) offHand.Unequip();
            return mainHand.TryEquip(item);
        }

        if (item.isWeapon)
        {
            if (mainHand.HasItem && mainHand.EquippedItem != null && (mainHand.EquippedItem.occupiesBothHands || mainHand.EquippedItem.handUsage == HandUsage.TwoHand))
            {
                mainHand.Unequip();
            }
            return mainHand.TryEquip(item);
        }
        else
        {
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
        if (slotType == EquipmentType.Ring)
        {
            // Prefer unequipping ring1; if empty, unequip ring2
            if (!ring1.IsEmpty) return ring1.Unequip();
            if (!ring2.IsEmpty) return ring2.Unequip();
            return null;
        }
        var slot = GetSlot(slotType);
        return slot != null ? slot.Unequip() : null;
    }

    public EquipmentSlot GetSlot(EquipmentType slotType)
    {
        // Resilient mapping even if slotMap is not initialized yet
        switch (slotType)
        {
            case EquipmentType.Helmet: return helmet;
            case EquipmentType.BodyArmour: return bodyArmour;
            case EquipmentType.Amulet: return amulet;
            case EquipmentType.Gloves: return gloves;
            case EquipmentType.Ring: return ring1; // default
            case EquipmentType.Boots: return boots;
            case EquipmentType.Belt: return belt;
            case EquipmentType.MainHand: return mainHand;
            case EquipmentType.OffHand: return offHand;
            default:
                if (slotMap != null && slotMap.TryGetValue(slotType, out var slot)) return slot;
                return null;
        }
    }

    public List<EquipmentData> GetAllEquippedItems()
    {
        var list = new List<EquipmentData>();
        foreach (var s in new[] { helmet, bodyArmour, amulet, gloves, ring1, ring2, boots, belt, mainHand, offHand })
        {
            if (s.HasItem) list.Add(s.EquippedItem);
        }
        return list;
    }

    public List<StatModifier> GetAllStatModifiers()
    {
        var all = new List<StatModifier>();
        foreach (var item in GetAllEquippedItems()) all.AddRange(item.AllStats);
        return all;
    }

    public bool HasItemEquipped(EquipmentData item) => GetAllEquippedItems().Contains(item);
    public int GetEquippedItemCount() => GetAllEquippedItems().Count;

    private void OnItemEquipped(EquipmentData item) => onEquipmentChanged?.Invoke();
    private void OnItemUnequipped(EquipmentData item) => onEquipmentChanged?.Invoke();

    [ContextMenu("Print Equipment Status")]
    public void PrintEquipmentStatus()
    {
        Debug.Log("=== EQUIPMENT STATUS ===");
        foreach (var s in new[] { helmet, bodyArmour, amulet, gloves, ring1, ring2, boots, belt, mainHand, offHand })
        {
            string status = s.HasItem ? s.EquippedItem.equipmentName : "Empty";
            Debug.Log($"{s.slotName}: {status}");
        }
    }
}
