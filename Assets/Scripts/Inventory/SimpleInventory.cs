using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "SimpleInventory", menuName = "POE Game/Inventory/Simple Inventory Data")]
public class SimpleInventory : ScriptableObject
{
    public EquipmentData[] slots; // fixed size array set in inspector

    [System.Serializable]
    public class InventoryChangedEvent : UnityEvent { }
    public InventoryChangedEvent onChanged = new InventoryChangedEvent();

#if UNITY_EDITOR
    private void OnValidate()
    {
        // notify editor UI when data changes
        onChanged.Invoke();
    }
#endif

    public int Capacity => slots != null ? slots.Length : 0;

    public EquipmentData Get(int index) => (slots != null && index >= 0 && index < slots.Length) ? slots[index] : null;

    public void Set(int index, EquipmentData item)
    {
        if (slots == null || index < 0 || index >= slots.Length) return;
        slots[index] = item;
        onChanged.Invoke();
    }

    public int FindFirstEmpty()
    {
        if (slots == null) return -1;
        for (int i = 0; i < slots.Length; i++) if (slots[i] == null) return i;
        return -1;
    }
}

