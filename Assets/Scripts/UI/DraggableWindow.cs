using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// Make a UI panel behave like a draggable window.
/// Attach to the root RectTransform of your Character screen (or any panel).
/// Optionally assign a dragHandle to restrict dragging to a header area.
public class DraggableWindow : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerUpHandler
{
    [Tooltip("RectTransform to move. Defaults to this component's RectTransform.")]
    public RectTransform window;

    [Tooltip("Optional: Only allow dragging when pointer is over this handle (e.g., title bar).")]
    public RectTransform dragHandle;

    [Tooltip("Optional: Close button to hide this window")] public Button closeButton;

    [Tooltip("Canvas the window belongs to. If null, will search upwards.")]
    public Canvas canvas;

    [Tooltip("Keep the window inside the visible canvas area.")]
    public bool clampToCanvas = true;

    [Header("Fit To Screen")]
    [Tooltip("Automatically ensure the window fits the current canvas size")] public bool fitToCanvas = true;
    [Tooltip("Use uniform scale to fit instead of resizing the Rect")] public bool scaleToFit = true;
    [Tooltip("Allow scaling above the original size when there is more space")] public bool allowUpscale = false;
    public Vector2 fitPadding = new Vector2(40f, 40f);

    private RectTransform _canvasRect;
    private Vector2 _startPointerLocal;
    private Vector2 _startAnchored;
    private bool _dragging;
    private Vector3 _initialScale = Vector3.one;
    private bool _pushedOnDown;
    private bool _pushedOnDrag;

    void Awake()
    {
        if (window == null) window = GetComponent<RectTransform>();
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (canvas != null) _canvasRect = canvas.transform as RectTransform;
        if (window != null) _initialScale = window.localScale;
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (fitToCanvas) FitToCanvasNow();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Bring to front when clicked
        if (window != null) window.SetAsLastSibling();
        // Block click-to-shoot when pressing on the drag handle even if no drag occurs
        if (IsOverHandle(eventData))
        {
            UIInputBlocker.PushBlock();
            _pushedOnDown = true;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!IsDragAllowed(eventData)) return;
        if (_canvasRect == null) return;
        _dragging = true;
        if (!_pushedOnDown)
        {
            UIInputBlocker.PushBlock();
            _pushedOnDrag = true;
        }
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect,
            eventData.position,
            canvas != null ? canvas.worldCamera : null,
            out _startPointerLocal
        );
        _startAnchored = window.anchoredPosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_dragging || _canvasRect == null) return;
        Vector2 currentPointerLocal;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect,
            eventData.position,
            canvas != null ? canvas.worldCamera : null,
            out currentPointerLocal)) return;

        Vector2 delta = currentPointerLocal - _startPointerLocal;
        Vector2 target = _startAnchored + delta;

        if (clampToCanvas)
        {
            target = ClampToCanvas(window, _canvasRect, target);
        }

        window.anchoredPosition = target;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_dragging)
        {
            _dragging = false;
            if (_pushedOnDrag)
            {
                UIInputBlocker.PopBlock();
                _pushedOnDrag = false;
            }
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_pushedOnDown)
        {
            UIInputBlocker.PopBlock();
            _pushedOnDown = false;
        }
    }

    void OnEnable()
    {
        if (fitToCanvas) FitToCanvasNow();
    }

    void OnDestroy()
    {
        if (closeButton != null) closeButton.onClick.RemoveListener(Close);
    }

    void OnRectTransformDimensionsChange()
    {
        if (!isActiveAndEnabled) return;
        if (fitToCanvas) FitToCanvasNow();
    }

    void Close()
    {
        if (window != null) window.gameObject.SetActive(false);
    }

    void FitToCanvasNow()
    {
        if (_canvasRect == null || window == null) return;
        var c = _canvasRect.rect;
        Vector2 maxSize = new Vector2(Mathf.Max(1f, c.width - fitPadding.x), Mathf.Max(1f, c.height - fitPadding.y));
        if (scaleToFit)
        {
            var baseScale = _initialScale.x; // assume uniform
            Vector2 size = window.rect.size * baseScale;
            float sx = maxSize.x / Mathf.Max(1f, size.x);
            float sy = maxSize.y / Mathf.Max(1f, size.y);
            float s = Mathf.Min(sx, sy);
            if (!allowUpscale) s = Mathf.Min(1f, s);
            window.localScale = new Vector3(_initialScale.x * s, _initialScale.y * s, 1f);
        }
        else
        {
            Vector2 size = window.rect.size;
            Vector2 clamped = new Vector2(Mathf.Min(size.x, maxSize.x), Mathf.Min(size.y, maxSize.y));
            Vector2 delta = clamped - size;
            window.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x + delta.x);
            window.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y + delta.y);
        }
        if (clampToCanvas) window.anchoredPosition = ClampToCanvas(window, _canvasRect, window.anchoredPosition);
    }

    bool IsDragAllowed(PointerEventData eventData)
    {
        if (dragHandle == null) return true;
        // Only start drag if pointer is over the handle
        return IsOverHandle(eventData);
    }

    bool IsOverHandle(PointerEventData eventData)
    {
        if (dragHandle == null) return true;
        return RectTransformUtility.RectangleContainsScreenPoint(
            dragHandle,
            eventData.position,
            canvas != null ? canvas.worldCamera : null
        );
    }

    static Vector2 ClampToCanvas(RectTransform window, RectTransform canvasRect, Vector2 desiredAnchored)
    {
        // Compute window size relative to canvas, accounting for scale differences
        Vector2 pivot = window.pivot;
        Vector3 wScale = window.lossyScale;
        Vector3 cScale = canvasRect.lossyScale;
        Vector2 relScale = new Vector2(
            cScale.x != 0f ? wScale.x / cScale.x : 1f,
            cScale.y != 0f ? wScale.y / cScale.y : 1f
        );
        Vector2 size = new Vector2(window.rect.width * relScale.x, window.rect.height * relScale.y);

        // Canvas rect in local coords
        Rect cRect = canvasRect.rect;

        // Convert desired anchored position into window bounds relative to canvas
        float left = desiredAnchored.x - pivot.x * size.x;
        float right = desiredAnchored.x + (1f - pivot.x) * size.x;
        float bottom = desiredAnchored.y - pivot.y * size.y;
        float top = desiredAnchored.y + (1f - pivot.y) * size.y;

        float dx = 0f, dy = 0f;
        if (left < cRect.xMin) dx = cRect.xMin - left;
        else if (right > cRect.xMax) dx = cRect.xMax - right;
        if (bottom < cRect.yMin) dy = cRect.yMin - bottom;
        else if (top > cRect.yMax) dy = cRect.yMax - top;

        return desiredAnchored + new Vector2(dx, dy);
    }
}


