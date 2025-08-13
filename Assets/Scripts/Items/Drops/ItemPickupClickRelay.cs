using UnityEngine;
using UnityEngine.EventSystems;

public class ItemPickupClickRelay : MonoBehaviour, IPointerClickHandler
{
    public ItemPickup target;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (target == null)
        {
            target = GetComponentInParent<ItemPickup>();
        }
        if (target == null) return;
        target.OnPointerClick(eventData);
    }
}


