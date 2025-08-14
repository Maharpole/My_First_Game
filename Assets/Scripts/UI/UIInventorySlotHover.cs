using UnityEngine;
using UnityEngine.EventSystems;

public class UIInventorySlotHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    public SimpleInventory inventory;
    public int slotIndex;

    public void OnPointerEnter(PointerEventData eventData)
    {
        var data = ResolveItem();
        if (data != null)
        {
            UITooltip.ShowEquipment(data, eventData.position);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UITooltip.Hide();
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        UITooltip.Move(eventData.position);
    }

    EquipmentData ResolveItem()
    {
        if (inventory == null || slotIndex < 0 || slotIndex >= inventory.Capacity) return null;
        return inventory.Get(slotIndex);
    }
}



