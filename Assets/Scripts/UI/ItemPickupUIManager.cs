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
        }
        ResolveOverlaps();
    }

    void UpdateEntryTransform(Entry e, ItemPickup item)
    {
        var worldPos = item.transform.position + Vector3.up * item.nameHeight;
        if (_cam == null)
        {
            e.root.gameObject.SetActive(false);
            return;
        }
        // Use WorldToScreenPoint to get z as well
        var sp = _cam.WorldToScreenPoint(worldPos); // Vector3
        var rt = e.root;
        var screenPos = sp;
        screenPos.x += screenOffset.x;
        screenPos.y += screenOffset.y;
        screenPos.z = 0f;
        rt.position = screenPos;
        // cull offscreen
        if (cullOffscreen)
        {
            bool visible = sp.z > 0 && sp.x >= 0 && sp.y >= 0 && sp.x <= Screen.width && sp.y <= Screen.height;
            if (e.root.gameObject.activeSelf != visible) e.root.gameObject.SetActive(visible);
        }
        else if (!e.root.gameObject.activeSelf)
        {
            e.root.gameObject.SetActive(true);
        }
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

    void ResolveOverlaps()
    {
        // Simple pass: nudge overlapping rects downward until separated and inside screen margins
        var list = new List<Entry>(_entries.Values);
        for (int i = 0; i < list.Count; i++)
        {
            var a = list[i];
            if (a == null || a.root == null || !a.root.gameObject.activeInHierarchy) continue;
            var art = a.root;
            for (int j = i + 1; j < list.Count; j++)
            {
                var b = list[j];
                if (b == null || b.root == null || !b.root.gameObject.activeInHierarchy) continue;
                var brt = b.root;
                if (RectOverlaps(art, brt))
                {
                    // Nudge b downward
                    var p = brt.position;
                    p.y -= Mathf.Max(minSeparation.y, brt.rect.height * 0.2f);
                    brt.position = p;
                }
            }
            // Clamp inside screen margins
            var pos = art.position;
            pos.x = Mathf.Clamp(pos.x, margins.xMin, Screen.width - margins.xMax);
            pos.y = Mathf.Clamp(pos.y, margins.yMin, Screen.height - margins.yMax);
            art.position = pos;
        }
    }

    bool RectOverlaps(RectTransform a, RectTransform b)
    {
        var ar = GetScreenRect(a);
        var br = GetScreenRect(b);
        return ar.Overlaps(br);
    }

    Rect GetScreenRect(RectTransform rt)
    {
        var p = rt.position;
        var size = rt.rect.size;
        // Anchor pivot assumed center; adjust rect
        return new Rect(p.x - size.x * 0.5f, p.y - size.y * 0.5f, size.x, size.y);
    }

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
        _entries[item] = new Entry { root = rt, label = txt, buttonImage = go.GetComponent<Image>() };
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
    }
}


