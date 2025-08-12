using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class SimpleInventoryUI : MonoBehaviour
{
    [Header("Data")] public SimpleInventory inventoryData; // ScriptableObject inventory

    [Header("UI")] public RectTransform gridParent; // container with GridLayoutGroup
    public GameObject slotButtonPrefab; // Button with child Image for icon

    [Header("Equipment")] public CharacterEquipment equipment; // optional (click-to-equip)

    private Button[] slotButtons;

    void Awake()
    {
        if (equipment == null) equipment = FindObjectOfType<CharacterEquipment>();
        RebuildGrid();
        if (inventoryData != null) inventoryData.onChanged.AddListener(Refresh);
        Refresh();
        Debug.Log($"[InvUI] Awake. inventoryData={(inventoryData!=null)}, equipment={(equipment!=null)}, gridParent={(gridParent!=null)}");
    }

    void OnDestroy()
    {
        if (inventoryData != null) inventoryData.onChanged.RemoveListener(Refresh);
        Debug.Log("[InvUI] OnDestroy");
    }

    public void RebuildGrid()
    {
        if (gridParent == null || slotButtonPrefab == null || inventoryData == null) return;
        foreach (Transform child in gridParent)
        {
            if (Application.isPlaying) Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
        }
        slotButtons = new Button[inventoryData.Capacity];

        for (int i = 0; i < inventoryData.Capacity; i++)
        {
            var go = Instantiate(slotButtonPrefab, gridParent);
            go.name = $"InvSlot_{i}";

            var btn = go.GetComponent<Button>();
            int idx = i;
            // Drag-only: do not hook click to equip
            slotButtons[i] = btn;

            Image iconImg = null;
            var vis = go.GetComponent<InventorySlotVisual>();
            if (vis != null && vis.icon != null) iconImg = vis.icon;
            if (iconImg == null) iconImg = go.GetComponentInChildren<Image>();

            var src = go.AddComponent<UIDragSourceSlot>();
            src.inventory = inventoryData;
            src.slotIndex = idx;
            src.iconImage = iconImg;
            src.onSuccessfulDrop = () => Refresh();

            var dst = go.AddComponent<UIDropTargetSlot>();
            dst.inventory = inventoryData;
            dst.slotIndex = idx;
            dst.onChanged = () => Refresh();
            // Ensure each slot has a CanvasGroup for non-destructive visual fading if needed
            if (go.GetComponent<CanvasGroup>() == null) go.AddComponent<CanvasGroup>();
            Debug.Log($"[InvUI] Built slot {idx}. icon={(iconImg!=null)} btn={(btn!=null)}");
        }
        Debug.Log($"[InvUI] Rebuilt grid with {inventoryData.Capacity} slots.");
    }

    public void Refresh()
    {
        if (slotButtons == null) return;
        Debug.Log("[InvUI] Refresh");
        for (int i = 0; i < slotButtons.Length; i++)
        {
            Image img = null;
            var vis = slotButtons[i] != null ? slotButtons[i].GetComponent<InventorySlotVisual>() : null;
            if (vis != null) img = vis.icon;
            if (img == null && slotButtons[i] != null) img = slotButtons[i].GetComponentInChildren<Image>();

            var item = inventoryData.Get(i);
            Debug.Log($"[InvUI] Slot {i} item={(item!=null?item.equipmentName:"(empty)")}");
            if (img != null)
            {
                img.sprite = item != null ? item.icon : null;
                img.color = item != null ? Color.white : new Color(1, 1, 1, 0.15f);
            }
        }
    }

    void OnSlotClicked(int index)
    {
        // Intentionally left blank: equipping is drag-and-drop only.
    }
}

public class UIDragSourceSlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public static bool IsDragInProgress { get; private set; }
    public SimpleInventory inventory;
    public int slotIndex;
    public Image iconImage;
    public System.Action onSuccessfulDrop;

    private RectTransform iconRT;
    private Vector2 startPos;
    private bool dragging;
    private Transform originalParent;

    void Start()
    {
        EnsureIconRT();
    }

    void EnsureIconRT()
    {
        if (iconRT != null) return;
        if (iconImage == null)
        {
            var vis = GetComponent<InventorySlotVisual>();
            if (vis != null && vis.icon != null) iconImage = vis.icon;
            if (iconImage == null) iconImage = GetComponentInChildren<Image>();
        }
        if (iconImage != null) iconRT = iconImage.rectTransform;
        if (iconRT == null)
        {
            Debug.LogWarning($"[Drag] No icon assigned for slot {slotIndex}. Add InventorySlotVisual and assign icon Image.");
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (inventory == null) return;
        EnsureIconRT();
        if (iconRT == null) return;
            if (inventory.Get(slotIndex) == null) { Debug.Log($"[Drag] Begin blocked: empty slot {slotIndex}."); return; }
        startPos = iconRT.position;
        if (iconImage != null) iconImage.raycastTarget = false;
        dragging = true;
        IsDragInProgress = true;
            Debug.Log($"[Drag] Begin slot {slotIndex} item={inventory.Get(slotIndex)?.equipmentName}");

        originalParent = iconRT.parent;
        var topCanvas = GetTopCanvas();
        if (topCanvas != null) iconRT.SetParent(topCanvas.transform, true);
        iconRT.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging || iconRT == null) return;
#if ENABLE_INPUT_SYSTEM
        var screenPos = Mouse.current != null ? (Vector2)Mouse.current.position.ReadValue() : eventData.position;
#else
        var screenPos = eventData.position;
#endif
        iconRT.position = screenPos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!dragging) return;
        dragging = false;
        IsDragInProgress = false;

        bool placed = false;
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
            Debug.Log($"[Drag] End slot {slotIndex}. Raycast hits={results.Count}");
        foreach (var r in results)
        {
            var dst = r.gameObject.GetComponentInParent<UIDropTargetSlot>();
                if (dst != null && dst.inventory == inventory) { Debug.Log($"[Drag] Drop over inventory cell {dst.slotIndex}"); placed = dst.AcceptDropFrom(slotIndex); if (placed) break; }
            var equipSlot = r.gameObject.GetComponentInParent<UIEquipmentSlot>();
            if (equipSlot != null)
            {
                var item = inventory.Get(slotIndex);
                    Debug.Log($"[Drag] Drop over equipment slot {equipSlot.slotType} (secondRing={equipSlot.useSecondRing}) item={(item!=null?item.equipmentName:"null")}");
                    if (item != null && equipSlot.TryEquipToSlot(item)) { inventory.Set(slotIndex, null); placed = true; break; }
            }
        }

        if (iconRT != null && originalParent != null) iconRT.SetParent(originalParent, true);
        if (iconRT != null) iconRT.position = startPos;
        if (iconImage != null) iconImage.raycastTarget = true;
        if (placed) onSuccessfulDrop?.Invoke();
            Debug.Log($"[Drag] Complete slot {slotIndex}. placed={placed}");
    }

    Canvas GetTopCanvas()
    {
        // Prefer a dedicated drag canvas so dragged icons are always above UI
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
            DontDestroyOnLoad(dragCanvasGO);
            Debug.Log("[Drag] Created DragCanvas with sortingOrder=9999");
        }
        return dragCanvas;
    }
}

public class UIDropTargetSlot : MonoBehaviour
{
    public SimpleInventory inventory;
    public int slotIndex;
    public System.Action onChanged;

    public bool AcceptDropFrom(int sourceIndex)
    {
        if (inventory == null) return false;
        if (sourceIndex < 0 || sourceIndex >= inventory.Capacity) return false;
        if (slotIndex < 0 || slotIndex >= inventory.Capacity) return false;
        if (sourceIndex == slotIndex) return false;

        var srcItem = inventory.Get(sourceIndex);
        var dstItem = inventory.Get(slotIndex);
        inventory.Set(sourceIndex, dstItem);
        inventory.Set(slotIndex, srcItem);
        onChanged?.Invoke();
        return true;
    }

    public bool AcceptDropItem(EquipmentData data, bool requireEmpty = true)
    {
        if (inventory == null || data == null) return false;
        var dstItem = inventory.Get(slotIndex);
        if (requireEmpty && dstItem != null) return false;
        inventory.Set(slotIndex, data);
        onChanged?.Invoke();
        return true;
    }
}
