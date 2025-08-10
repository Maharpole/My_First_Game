using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class InventorySystem : MonoBehaviour
{
    public List<EquipmentData> items = new List<EquipmentData>();
    public int maxSlots = 60;

    [System.Serializable]
    public class InventoryEvent : UnityEvent { }

    public InventoryEvent onChanged = new InventoryEvent();

    public bool TryAdd(EquipmentData item)
    {
        if (item == null) return false;
        if (items.Count >= maxSlots) return false;
        items.Add(item);
        onChanged?.Invoke();
        return true;
    }

    public bool Remove(EquipmentData item)
    {
        bool ok = items.Remove(item);
        if (ok) onChanged?.Invoke();
        return ok;
    }

    public void Swap(int a, int b)
    {
        if (a < 0 || b < 0 || a >= items.Count || b >= items.Count) return;
        var tmp = items[a]; items[a] = items[b]; items[b] = tmp;
        onChanged?.Invoke();
    }
}
