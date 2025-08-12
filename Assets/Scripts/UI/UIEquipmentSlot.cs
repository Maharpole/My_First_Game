using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIEquipmentSlot : MonoBehaviour, IDropHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public EquipmentType slotType;
    public bool useSecondRing = false; // when slotType == Ring, choose ring2 if true

    public CharacterEquipment equipment;
    public SimpleInventory inventoryData;
    public Image icon;

    // Drag state
    private RectTransform iconRT;
    private Transform originalParent;
    private Vector2 startPos;
    private bool dragging;

    void Awake()
    {
        if (equipment == null) equipment = FindObjectOfType<CharacterEquipment>();
        EnsureIconRT();
    }

    void OnEnable()
    {
        if (equipment != null) equipment.onEquipmentChanged.AddListener(UpdateIcon);
        UpdateIcon();
    }

    void Start()
    {
        UpdateIcon();
    }

    void OnDisable()
    {
        if (equipment != null) equipment.onEquipmentChanged.RemoveListener(UpdateIcon);
    }

    EquipmentSlot ResolveSlot()
    {
        if (equipment == null) return null;
        if (slotType == EquipmentType.Ring)
        {
            return useSecondRing ? equipment.ring2 : equipment.ring1;
        }
        return equipment.GetSlot(slotType);
    }

    public EquipmentData GetCurrentData()
    {
        var slot = ResolveSlot();
        return (slot != null && slot.HasItem) ? slot.EquippedItem : null;
    }

    public void UpdateIcon()
    {
        EnsureIconRT();
        if (icon == null) return;
        var slot = ResolveSlot();
        var spr = (slot != null && slot.HasItem && slot.EquippedItem != null) ? slot.EquippedItem.icon : null;
        icon.sprite = spr;
        icon.color = spr != null ? Color.white : new Color(1f, 1f, 1f, 0.15f);
    }

    public bool TryEquipToSlot(EquipmentData data)
    {
        if (equipment == null || data == null) return false;
        var slot = ResolveSlot();
        if (slot == null || !slot.CanEquip(data)) return false;

        int sourceIndex = -1;
        if (inventoryData != null && inventoryData.slots != null)
        {
            for (int i = 0; i < inventoryData.slots.Length; i++)
            {
                if (inventoryData.slots[i] == data) { sourceIndex = i; break; }
            }
        }

        // Use CharacterEquipment to respect rules and retrieve swapped item
        bool ok;
        EquipmentData old = null;
        if (equipment != null)
        {
            ok = equipment.TryEquipFromInventory(data, out old);
            // If this is specifically the ring2 UI and ring1 grabbed the item, move it to ring2
            if (ok && slotType == EquipmentType.Ring && useSecondRing)
            {
                if (equipment.ring2.IsEmpty && !equipment.ring1.IsEmpty)
                {
                    var rItem = equipment.ring1.Unequip();
                    equipment.ring2.TryEquip(rItem);
                }
            }
        }
        else
        {
            if (slot.HasItem) old = slot.Unequip();
            ok = slot.TryEquip(data);
        }
        if (ok)
        {
            if (sourceIndex >= 0) inventoryData.Set(sourceIndex, old);
            UpdateIcon();
            return true;
        }
        else
        {
            if (old != null) slot.TryEquip(old);
            UpdateIcon();
            return false;
        }
    }

    public bool SwapWith(UIEquipmentSlot other)
    {
        if (other == null) return false;
        var mySlot = ResolveSlot();
        var otherSlot = other.ResolveSlot();
        if (mySlot == null || otherSlot == null) return false;

        var myData = mySlot.HasItem ? mySlot.EquippedItem : null;
        var otherData = otherSlot.HasItem ? otherSlot.EquippedItem : null;

        // Check compatibility
        if (otherData != null && !mySlot.CanEquip(otherData)) return false;
        if (myData != null && !otherSlot.CanEquip(myData)) return false;

        // Perform swap
        mySlot.Unequip();
        otherSlot.Unequip();
        if (otherData != null) mySlot.TryEquip(otherData);
        if (myData != null) otherSlot.TryEquip(myData);
        UpdateIcon();
        other.UpdateIcon();
        return true;
    }

    public void OnDrop(PointerEventData eventData)
    {
        // Optional: if other sources call OnDrop, we could handle here. Our drag sources manage equip/swap.
    }

    // === Drag from equipment to inventory or swap with other equipment ===
    public void OnBeginDrag(PointerEventData eventData)
    {
        var slot = ResolveSlot();
        if (slot == null || !slot.HasItem) return;
        EnsureIconRT();
        if (iconRT == null) return;

        startPos = iconRT.position;
        originalParent = iconRT.parent;
        if (icon != null) icon.raycastTarget = false;

        var topCanvas = GetTopCanvas();
        if (topCanvas != null) iconRT.SetParent(topCanvas.transform, true);
        iconRT.SetAsLastSibling();
        dragging = true;
        
        Debug.Log($"[EquipDrag] Begin {slotType} (ring2={useSecondRing}) item={slot.EquippedItem?.equipmentName}");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging || iconRT == null) return;
        iconRT.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!dragging) return;
        dragging = false;

        var mySlot = ResolveSlot();
        var myData = (mySlot != null && mySlot.HasItem) ? mySlot.EquippedItem : null;
        bool placed = false;

        var results = new System.Collections.Generic.List<RaycastResult>();
        if (EventSystem.current != null)
        {
            EventSystem.current.RaycastAll(eventData, results);
        }
        
        foreach (var r in results)
        {
            // Drop to inventory cell (requires empty)
            var invDst = r.gameObject.GetComponentInParent<UIDropTargetSlot>();
            if (!placed && invDst != null && inventoryData != null && invDst.inventory == inventoryData && myData != null)
            {
                if (invDst.AcceptDropItem(myData, true))
                {
                    mySlot.Unequip();
                    placed = true;
                    Debug.Log($"[EquipDrag] Placed into inventory slot {invDst.slotIndex}");
                    break;
                }
            }

            // Swap with another equipment slot
            var otherEquip = r.gameObject.GetComponentInParent<UIEquipmentSlot>();
            if (!placed && otherEquip != null && otherEquip != this)
            {
                var otherSlot = otherEquip.ResolveSlot();
                if (otherSlot != null)
                {
                    var otherData = otherSlot.HasItem ? otherSlot.EquippedItem : null;
                    if ((otherData == null || ResolveSlot().CanEquip(otherData)) && (myData == null || otherSlot.CanEquip(myData)))
                    {
                        if (mySlot.HasItem) mySlot.Unequip();
                        if (otherSlot.HasItem) otherSlot.Unequip();
                        if (otherData != null) ResolveSlot().TryEquip(otherData);
                        if (myData != null) otherSlot.TryEquip(myData);
                        placed = true;
                        otherEquip.UpdateIcon();
                        Debug.Log("[EquipDrag] Swapped equipment items");
                        break;
                    }
                }
            }
        }

        // Restore icon visuals
        if (iconRT != null && originalParent != null) iconRT.SetParent(originalParent, true);
        if (iconRT != null) iconRT.position = startPos;
        if (icon != null) icon.raycastTarget = true;
        UpdateIcon();
        Debug.Log($"[EquipDrag] End placed={placed}");
    }

    void EnsureIconRT()
    {
        if (icon == null)
        {
            icon = GetComponentInChildren<Image>();
        }
        if (icon != null && iconRT == null)
        {
            iconRT = icon.rectTransform;
        }
    }

    Canvas GetTopCanvas()
    {
        var dragCanvasGO = GameObject.Find("DragCanvas");
        Canvas dragCanvas = null;
        if (dragCanvasGO != null)
        {
            dragCanvas = dragCanvasGO.GetComponent<Canvas>();
        }
        if (dragCanvas == null)
        {
            dragCanvasGO = new GameObject("DragCanvas");
            dragCanvas = dragCanvasGO.AddComponent<Canvas>();
            dragCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            dragCanvas.sortingOrder = 9999;
            dragCanvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            Object.DontDestroyOnLoad(dragCanvasGO);
            Debug.Log("[EquipDrag] Created DragCanvas");
        }
        return dragCanvas;
    }
}
