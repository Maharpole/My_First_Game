using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemPickupUIManager : MonoBehaviour
{
    public static ItemPickupUIManager Instance { get; private set; }

    [Header("UI")]
    [Tooltip("Screen Space Overlay Canvas to host pickup labels. If null, one will be created at runtime.")]
    public Canvas overlayCanvas;
    [Tooltip("UI prefab with a Button and TMP_Text to represent a pickup label.")]
    public GameObject pickupEntryPrefab;
    [Tooltip("Screen-space offset for labels (pixels)")] public Vector2 screenOffset = new Vector2(0f, 24f);
    [Header("Layout")]
    [Tooltip("Minimum separation between entries (pixels)")] public Vector2 minSeparation = new Vector2(8f, 8f);
    [Tooltip("Viewport margins (pixels)")] public Rect margins = new Rect(8, 8, 8, 8);
    [Tooltip("Hide entries when off-screen or behind camera")] public bool cullOffscreen = true;

    [Header("Sizing")]
    [Tooltip("Padding around text for the background (pixels)")] public Vector2 backgroundPadding = new Vector2(16f, 8f);
    [Tooltip("Automatically size background to text preferred size")] public bool autoSizeToText = true;

    private readonly Dictionary<ItemPickup, Entry> _entries = new Dictionary<ItemPickup, Entry>();
    private Camera _cam;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Ensure persistence: apply to root to satisfy DontDestroyOnLoad requirements
        var root = transform.root != null ? transform.root.gameObject : gameObject;
        if (root.transform.parent == null)
        {
            DontDestroyOnLoad(root);
        }

        _cam = Camera.main;
        if (overlayCanvas == null)
        {
            var go = new GameObject("PickupOverlayCanvas");
            overlayCanvas = go.AddComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.sortingOrder = 5000;
            go.AddComponent<GraphicRaycaster>();
            DontDestroyOnLoad(go);
        }
    }

    void LateUpdate()
    {
        if (_cam == null) _cam = Camera.main;
        foreach (var kvp in _entries)
        {
            var item = kvp.Key;
            var e = kvp.Value;
            if (item == null || e == null || e.root == null) continue;
            UpdateEntryTransform(e, item);
            UpdateEntryText(e, item);
            UpdateEntryStyle(e, item);
            UpdateEntrySize(e);
        }
        // Overlap resolution removed to avoid flashing/jitter
    }

    void UpdateEntryTransform(Entry e, ItemPickup item)
    {
        var worldPos = item.transform.position + Vector3.up * item.nameHeight;
        if (_cam == null)
        {
            SetEntryVisible(e, false);
            return;
        }
        // Screen point with depth; ignore when behind camera to avoid wild screen coords
        var sp = _cam.WorldToScreenPoint(worldPos);
        if (sp.z <= 0f)
        {
            SetEntryVisible(e, false);
            return;
        }

        // Visibility check before positioning
        bool onScreen = sp.x >= 0 && sp.y >= 0 && sp.x <= Screen.width && sp.y <= Screen.height;
        if (cullOffscreen && !onScreen)
        {
            SetEntryVisible(e, false);
            return; // keep last good position while hidden
        }

        // Convert to local position relative to the overlay canvas rect to avoid scaler jitter
        var canvasRt = overlayCanvas != null ? overlayCanvas.transform as RectTransform : null;
        var rt = e.root;
        Vector2 screenPt = new Vector2(sp.x + screenOffset.x, sp.y + screenOffset.y);
        if (canvasRt != null)
        {
            Vector2 localPoint;
            Camera camForCanvas = null;
            if (overlayCanvas.renderMode == RenderMode.ScreenSpaceCamera || overlayCanvas.renderMode == RenderMode.WorldSpace)
            {
                camForCanvas = overlayCanvas.worldCamera != null ? overlayCanvas.worldCamera : _cam;
            }
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screenPt, camForCanvas, out localPoint);
            rt.anchoredPosition = localPoint;
        }
        else
        {
            // Fallback: set world position (should not happen since we create the canvas)
            rt.position = new Vector3(screenPt.x, screenPt.y, 0f);
        }

        // Show once we have a valid on-screen position
        SetEntryVisible(e, true);
    }

    void UpdateEntryText(Entry e, ItemPickup item)
    {
        if (e.label == null) return;
        var desired = item != null ? item.GetDisplayNameForUI() : string.Empty;
        if (e.label.text != desired)
        {
            e.label.text = desired;
        }
    }

    void UpdateEntryStyle(Entry e, ItemPickup item)
    {
        if (e.label == null) return;
        // Button background color to rarity
        if (e.buttonImage == null)
        {
            var img = e.root.GetComponent<Image>();
            e.buttonImage = img;
        }
        if (e.buttonImage != null)
        {
            e.buttonImage.color = item.GetRarityColorForUI();
        }
    }

    // Overlap fixing logic removed by request

    public void Register(ItemPickup item)
    {
        if (item == null || _entries.ContainsKey(item)) return;
        if (pickupEntryPrefab == null || overlayCanvas == null)
        {
            Debug.LogWarning("[ItemPickupUI] Missing entry prefab or overlay canvas.");
            return;
        }
        var go = Instantiate(pickupEntryPrefab, overlayCanvas.transform);
        var rt = go.GetComponent<RectTransform>();
        var btn = go.GetComponentInChildren<Button>();
        var txt = go.GetComponentInChildren<TMP_Text>();
        // Ensure non-stretch anchors so width won't follow screen width
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero; // initialize deterministically
        }
        // Ensure a CanvasGroup to control visibility without flashing
        var cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        if (btn != null)
        {
            btn.onClick.AddListener(() =>
            {
                var player = Player.Instance ?? FindFirstObjectByType<Player>();
                if (player == null || item == null) return;
                // Ignore range when clicking via overlay to ensure reliable pickup
                item.TryPickup(player, float.PositiveInfinity);
            });
        }
        _entries[item] = new Entry { root = rt, label = txt, buttonImage = go.GetComponent<Image>(), canvasGroup = cg };
        // If background image exists, ensure its RectTransform is also non-stretch
        if (_entries[item].buttonImage != null)
        {
            var bgRt = _entries[item].buttonImage.rectTransform;
            bgRt.anchorMin = new Vector2(0.5f, 0.5f);
            bgRt.anchorMax = new Vector2(0.5f, 0.5f);
            bgRt.pivot = new Vector2(0.5f, 0.5f);
        }
        // Place immediately so first frame doesn't jump
        UpdateEntryText(_entries[item], item);
        UpdateEntryTransform(_entries[item], item);
        UpdateEntryStyle(_entries[item], item);
    }

    public void Unregister(ItemPickup item)
    {
        if (item == null) return;
        if (_entries.TryGetValue(item, out var e))
        {
            if (e != null && e.root != null) Destroy(e.root.gameObject);
            _entries.Remove(item);
        }
    }

    class Entry
    {
        public RectTransform root;
        public TMP_Text label;
        public Image buttonImage;
        public CanvasGroup canvasGroup;
    }

    void UpdateEntrySize(Entry e)
    {
        if (!autoSizeToText || e == null) return;
        var targetRt = e.buttonImage != null ? e.buttonImage.rectTransform : e.root;
        if (targetRt == null || e.label == null) return;
        // Calculate preferred size for current text
        e.label.ForceMeshUpdate();
        float w = e.label.preferredWidth;
        float h = e.label.preferredHeight;
        var size = new Vector2(w + backgroundPadding.x * 2f, h + backgroundPadding.y * 2f);
        targetRt.sizeDelta = size;
    }

    void SetEntryVisible(Entry e, bool visible)
    {
        if (e == null) return;
        var cg = e.canvasGroup;
        if (cg == null)
        {
            cg = e.root != null ? e.root.GetComponent<CanvasGroup>() : null;
            if (cg == null && e.root != null) cg = e.root.gameObject.AddComponent<CanvasGroup>();
            e.canvasGroup = cg;
        }
        if (cg != null)
        {
            cg.alpha = visible ? 1f : 0f;
            cg.blocksRaycasts = visible;
        }
    }
}



