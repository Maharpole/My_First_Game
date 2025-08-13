using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

internal static class InventoryUILogging
{
	public static bool Enabled = false; // non-const to avoid CS0162 unreachable warnings from constant-folded branches
}

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
        if (InventoryUILogging.Enabled) Debug.Log($"[InvUI] Awake. inventoryData={(inventoryData!=null)}, equipment={(equipment!=null)}, gridParent={(gridParent!=null)}");
    }

    void OnDestroy()
    {
        if (inventoryData != null) inventoryData.onChanged.RemoveListener(Refresh);
        if (InventoryUILogging.Enabled) Debug.Log("[InvUI] OnDestroy");
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
            if (InventoryUILogging.Enabled) Debug.Log($"[InvUI] Built slot {idx}. icon={(iconImg!=null)} btn={(btn!=null)}");
        }
        if (InventoryUILogging.Enabled) Debug.Log($"[InvUI] Rebuilt grid with {inventoryData.Capacity} slots.");
    }

    public void Refresh()
    {
        if (slotButtons == null) return;
        if (InventoryUILogging.Enabled) Debug.Log("[InvUI] Refresh");
        for (int i = 0; i < slotButtons.Length; i++)
        {
            Image img = null;
            var vis = slotButtons[i] != null ? slotButtons[i].GetComponent<InventorySlotVisual>() : null;
            if (vis != null) img = vis.icon;
            if (img == null && slotButtons[i] != null) img = slotButtons[i].GetComponentInChildren<Image>();

            var item = inventoryData.Get(i);
            if (InventoryUILogging.Enabled) Debug.Log($"[InvUI] Slot {i} item={(item!=null?item.equipmentName:"(empty)")}");
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

public class UIDragSourceSlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler
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
    private Image dragGhost;
    private CanvasGroup slotCanvasGroup;
    private Vector2 currentPointerPos;
    private Canvas dragCanvasRef;
    private bool ghostCreated;
    private bool didBeginDrag;

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
            if (InventoryUILogging.Enabled) Debug.LogWarning($"[Drag] No icon assigned for slot {slotIndex}. Add InventorySlotVisual and assign icon Image.");
        }
        if (slotCanvasGroup == null) slotCanvasGroup = GetComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (inventory == null) return;
        EnsureIconRT();
        if (iconRT == null) return;
        if (inventory.Get(slotIndex) == null) { if (InventoryUILogging.Enabled) Debug.Log($"[Drag] Begin blocked: empty slot {slotIndex}."); return; }
        didBeginDrag = true;
        if (!ghostCreated)
        {
            startPos = iconRT.position;
            if (iconImage != null)
            {
                iconImage.raycastTarget = false;
                iconImage.enabled = false; // keep layout stable
            }
            if (slotCanvasGroup != null) slotCanvasGroup.blocksRaycasts = false;
            CreateGhost();
            ghostCreated = true;
        }
        dragging = true;
        IsDragInProgress = true;
        if (InventoryUILogging.Enabled) Debug.Log($"[Drag] Begin slot {slotIndex} item={inventory.Get(slotIndex)?.equipmentName}");
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Intentionally minimal; Update() will poll pointer each frame for responsiveness
        if (!dragging) return;
    }

    void Update()
    {
        if (!dragging || dragGhost == null) return;
#if ENABLE_INPUT_SYSTEM
		var screenPos = Mouse.current != null ? (Vector2)Mouse.current.position.ReadValue() : (Vector2)Input.mousePosition;
#else
		var screenPos = (Vector2)Input.mousePosition;
#endif
        PositionGhost(screenPos);

		// If the primary button was released but OnEndDrag wasn't delivered, finish drop here
#if ENABLE_INPUT_SYSTEM
		bool released = Mouse.current != null ? !Mouse.current.leftButton.isPressed : !Input.GetMouseButton(0);
#else
		bool released = !Input.GetMouseButton(0);
#endif
		if (released)
		{
			FinishDragAt(screenPos);
			return;
		}

		// Optional cancel with Escape
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			CancelDrag();
		}
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (inventory == null) return;
        EnsureIconRT();
        if (iconRT == null) return;
        if (inventory.Get(slotIndex) == null) return;
        if (!ghostCreated)
        {
            startPos = iconRT.position;
            if (iconImage != null)
            {
                iconImage.raycastTarget = false;
                iconImage.enabled = false;
            }
            if (slotCanvasGroup != null) slotCanvasGroup.blocksRaycasts = false;
            CreateGhost();
            ghostCreated = true;
        }
        dragging = true;
        IsDragInProgress = true;
        // Snap immediately on press
        PositionGhost(eventData.position);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // If a drag never actually began (threshold not met), clean up immediately
        if (!didBeginDrag && ghostCreated)
        {
            CleanupGhostAndRestore();
            dragging = false;
            IsDragInProgress = false;
        }
        didBeginDrag = false;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
		if (!dragging) return;
		FinishDragAt(eventData.position);
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

    void FinishDragAt(Vector2 screenPos)
    {
        if (!dragging) return;
        dragging = false;
        IsDragInProgress = false;

        bool placed = false;
        var results = new System.Collections.Generic.List<RaycastResult>();
        var ped = new PointerEventData(EventSystem.current) { position = screenPos };
        EventSystem.current.RaycastAll(ped, results);
        if (InventoryUILogging.Enabled) Debug.Log($"[Drag] End slot {slotIndex}. Raycast hits={results.Count}");
        foreach (var r in results)
        {
            var dst = r.gameObject.GetComponentInParent<UIDropTargetSlot>();
            if (dst != null && dst.inventory == inventory) { if (InventoryUILogging.Enabled) Debug.Log($"[Drag] Drop over inventory cell {dst.slotIndex}"); placed = dst.AcceptDropFrom(slotIndex); if (placed) break; }
            var equipSlot = r.gameObject.GetComponentInParent<UIEquipmentSlot>();
            if (equipSlot != null)
            {
                var item = inventory.Get(slotIndex);
                if (InventoryUILogging.Enabled) Debug.Log($"[Drag] Drop over equipment slot {equipSlot.slotType} (secondRing={equipSlot.useSecondRing}) item={(item!=null?item.equipmentName:"null")} ");
                if (item != null && equipSlot.TryEquipToSlot(item)) { inventory.Set(slotIndex, null); placed = true; break; }
            }
        }

        CleanupGhostAndRestore();
        if (placed) onSuccessfulDrop?.Invoke();
        if (InventoryUILogging.Enabled) Debug.Log($"[Drag] Complete slot {slotIndex}. placed={placed}");
    }

    void CancelDrag()
    {
        if (!dragging && !ghostCreated) return;
        dragging = false;
        IsDragInProgress = false;
        didBeginDrag = false;
        CleanupGhostAndRestore();
        if (InventoryUILogging.Enabled) Debug.Log($"[Drag] Cancel slot {slotIndex}");
    }

    void CreateGhost()
    {
        dragCanvasRef = GetTopCanvas();
        if (dragCanvasRef == null) return;
        EnsureCanvasScalerMatches(dragCanvasRef);
        var go = new GameObject($"DragGhost_{slotIndex}");
        go.transform.SetParent(dragCanvasRef.transform, false);
        dragGhost = go.AddComponent<Image>();
        dragGhost.raycastTarget = false;
        if (iconImage != null)
        {
            dragGhost.sprite = iconImage.sprite;
            dragGhost.color = iconImage.color;
            var ghRT = dragGhost.rectTransform;
            var srcRT = iconImage.rectTransform;
            ghRT.pivot = srcRT.pivot;
            ghRT.anchorMin = ghRT.anchorMax = new Vector2(0.5f, 0.5f);
            ghRT.sizeDelta = srcRT.rect.size;
        }
    }

    void CleanupGhostAndRestore()
    {
        if (dragGhost != null) Destroy(dragGhost.gameObject);
        dragGhost = null;
        ghostCreated = false;
        if (iconImage != null)
        {
            iconImage.enabled = true;
            iconImage.raycastTarget = true;
        }
        if (slotCanvasGroup != null) slotCanvasGroup.blocksRaycasts = true;
    }

    void EnsureCanvasScalerMatches(Canvas dragCanvas)
    {
        // Try to mirror the main UI CanvasScaler so sizing matches
        var refScaler = FindObjectOfType<CanvasScaler>();
        if (refScaler == null) return;
        var myScaler = dragCanvas.GetComponent<CanvasScaler>();
        if (myScaler == null) myScaler = dragCanvas.gameObject.AddComponent<CanvasScaler>();
        myScaler.uiScaleMode = refScaler.uiScaleMode;
        myScaler.referenceResolution = refScaler.referenceResolution;
        myScaler.matchWidthOrHeight = refScaler.matchWidthOrHeight;
        myScaler.screenMatchMode = refScaler.screenMatchMode;
        myScaler.physicalUnit = refScaler.physicalUnit;
        myScaler.fallbackScreenDPI = refScaler.fallbackScreenDPI;
        myScaler.defaultSpriteDPI = refScaler.defaultSpriteDPI;
        myScaler.dynamicPixelsPerUnit = refScaler.dynamicPixelsPerUnit;
    }

    void PositionGhost(Vector2 screenPos)
    {
        if (dragGhost == null || dragCanvasRef == null) return;
        if (dragCanvasRef.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            dragGhost.rectTransform.position = screenPos;
        }
        else
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                dragCanvasRef.transform as RectTransform,
                screenPos,
                dragCanvasRef.worldCamera,
                out var localPos
            );
            dragGhost.rectTransform.localPosition = localPos;
        }
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
