using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class UIEquipmentSlot : MonoBehaviour, IDropHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler
{
    public EquipmentType slotType;
    public bool useSecondRing = false; // when slotType == Ring, choose ring2 if true

    public CharacterEquipment equipment;
    public SimpleInventory inventoryData;
    public Image icon;
    [Header("Empty State")]
    public Sprite emptyIcon;
    public Color emptyIconColor = new Color(1f, 1f, 1f, 0.25f);

    // Drag state
    private RectTransform iconRT;
    private Transform originalParent;
    private Vector2 startPos;
    private bool dragging;
    private Image dragGhost;
    private CanvasGroup slotCanvasGroup;
    private Canvas dragCanvasRef;
    private bool ghostCreated;
    private bool didBeginDrag;

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
        var spr = (slot != null && slot.HasItem && slot.EquippedItem != null) ? slot.EquippedItem.icon : emptyIcon;
        if (spr == null)
        {
            // Guarantee a visible placeholder so users see an empty slot state even if not assigned in inspector
            spr = GetOrCreatePlaceholderSprite();
        }
        icon.sprite = spr;
        icon.color = (slot != null && slot.HasItem && slot.EquippedItem != null) ? Color.white : emptyIconColor;
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
    public void OnPointerDown(PointerEventData eventData)
    {
        var slot = ResolveSlot();
        if (slot == null || !slot.HasItem) return;
        EnsureIconRT();
        if (iconRT == null) return;
        if (!ghostCreated)
        {
            startPos = iconRT.position;
            originalParent = iconRT.parent;
            if (icon != null)
            {
                icon.raycastTarget = false;
                icon.enabled = false;
            }
            if (slotCanvasGroup != null) slotCanvasGroup.blocksRaycasts = false;
            CreateGhost();
            ghostCreated = true;
        }
        dragging = true;
        PositionGhost(eventData.position);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        var slot = ResolveSlot();
        if (slot == null || !slot.HasItem) return;
        didBeginDrag = true;
        dragging = true;
        Debug.Log($"[EquipDrag] Begin {slotType} (ring2={useSecondRing}) item={slot?.EquippedItem?.equipmentName}");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging) return;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!dragging) return;
        FinishDragAt(eventData.position);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!didBeginDrag && ghostCreated)
        {
            CancelDrag();
        }
        didBeginDrag = false;
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
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelDrag();
        }
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
        if (slotCanvasGroup == null) slotCanvasGroup = GetComponent<CanvasGroup>();
    }

    static Sprite _placeholder;
    static Texture2D _placeholderTex;
    static Sprite GetOrCreatePlaceholderSprite()
    {
        if (_placeholder != null) return _placeholder;
        const int size = 8;
        _placeholderTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var cols = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool chk = ((x ^ y) & 1) == 0;
                cols[y * size + x] = chk ? new Color(1f, 1f, 1f, 0.15f) : new Color(0.8f, 0.8f, 0.8f, 0.15f);
            }
        }
        _placeholderTex.SetPixels(cols);
        _placeholderTex.Apply();
        _placeholder = Sprite.Create(_placeholderTex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 8f);
        _placeholder.name = "EmptySlotPlaceholder";
        return _placeholder;
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

    void CreateGhost()
    {
        dragCanvasRef = GetTopCanvas();
        if (dragCanvasRef == null) return;
        EnsureCanvasScalerMatches(dragCanvasRef);
        var go = new GameObject($"EquipDragGhost_{slotType}");
        go.transform.SetParent(dragCanvasRef.transform, false);
        dragGhost = go.AddComponent<Image>();
        dragGhost.raycastTarget = false;
        if (icon != null)
        {
            dragGhost.sprite = icon.sprite;
            dragGhost.color = icon.color;
            var ghRT = dragGhost.rectTransform;
            var srcRT = icon.rectTransform;
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
        if (icon != null)
        {
            icon.enabled = true;
            icon.raycastTarget = true;
        }
        if (slotCanvasGroup != null) slotCanvasGroup.blocksRaycasts = true;
        if (iconRT != null && originalParent != null) iconRT.SetParent(originalParent, true);
        if (iconRT != null) iconRT.position = startPos;
        UpdateIcon();
    }

    void EnsureCanvasScalerMatches(Canvas dragCanvas)
    {
        var refScaler = Object.FindObjectOfType<CanvasScaler>();
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

    void FinishDragAt(Vector2 screenPos)
    {
        if (!dragging) return;
        dragging = false;

        var mySlot = ResolveSlot();
        var myData = (mySlot != null && mySlot.HasItem) ? mySlot.EquippedItem : null;
        bool placed = false;

        var results = new System.Collections.Generic.List<RaycastResult>();
        var ped = new PointerEventData(EventSystem.current) { position = screenPos };
        EventSystem.current.RaycastAll(ped, results);

        foreach (var r in results)
        {
            // Drop to inventory (empty only)
            var invDst = r.gameObject.GetComponentInParent<UIDropTargetSlot>();
            if (!placed && invDst != null && myData != null)
            {
                if (invDst.AcceptDropItem(myData, true))
                {
                    // Unequip after successful place to avoid item loss
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
                        // Swap with rollback safety
                        var myBefore = mySlot.HasItem ? mySlot.Unequip() : null;
                        var otherBefore = otherSlot.HasItem ? otherSlot.Unequip() : null;
                        bool okA = (otherBefore == null) || ResolveSlot().TryEquip(otherBefore);
                        bool okB = (myBefore == null) || otherSlot.TryEquip(myBefore);
                        if (!(okA && okB))
                        {
                            // rollback
                            if (ResolveSlot().IsEmpty && myBefore != null) ResolveSlot().TryEquip(myBefore);
                            if (otherSlot.IsEmpty && otherBefore != null) otherSlot.TryEquip(otherBefore);
                        }
                        placed = true;
                        otherEquip.UpdateIcon();
                        Debug.Log("[EquipDrag] Swapped equipment items");
                        break;
                    }
                }
            }
        }

        CleanupGhostAndRestore();
        Debug.Log($"[EquipDrag] End placed={placed}");
    }

    void CancelDrag()
    {
        if (!dragging && !ghostCreated) return;
        dragging = false;
        didBeginDrag = false;
        CleanupGhostAndRestore();
        Debug.Log($"[EquipDrag] Cancel {slotType}");
    }
}
