using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UITooltip : MonoBehaviour
{
    private static UITooltip _instance;
    public static UITooltip Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("UITooltip");
                _instance = go.AddComponent<UITooltip>();
                _instance.InitializeRuntimeUI();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private Canvas _canvas;
    private RectTransform _root;
    private Image _background;
    private TextMeshProUGUI _text;
    private bool _visible;
    private Vector2 _padding = new Vector2(12f, 8f);
    private Vector2 _offset = new Vector2(18f, -18f);
    private float _maxWidth = 420f;

    void InitializeRuntimeUI()
    {
        // Canvas
        var canvasGO = new GameObject("TooltipCanvas");
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 10000;
        canvasGO.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(canvasGO);

        // Root panel
        var rootGO = new GameObject("Panel");
        rootGO.transform.SetParent(canvasGO.transform, false);
        _root = rootGO.AddComponent<RectTransform>();
        _root.anchorMin = _root.anchorMax = new Vector2(0f, 1f);
        _root.pivot = new Vector2(0f, 1f);

        _background = rootGO.AddComponent<Image>();
        _background.color = new Color(0f, 0f, 0f, 0.85f);

        // Text
        var textGO = new GameObject("Text");
        textGO.transform.SetParent(rootGO.transform, false);
        _text = textGO.AddComponent<TextMeshProUGUI>();
        _text.richText = true;
        _text.textWrappingMode = TextWrappingModes.Normal;
        _text.fontSize = 20f;
        _text.color = Color.white;

        var textRT = _text.rectTransform;
        textRT.anchorMin = new Vector2(0f, 1f);
        textRT.anchorMax = new Vector2(0f, 1f);
        textRT.pivot = new Vector2(0f, 1f);

        SetActive(false);
    }

    void SetActive(bool state)
    {
        _visible = state;
        if (_root != null) _root.gameObject.SetActive(state);
        if (_canvas != null) _canvas.gameObject.SetActive(state);
    }

    public static void ShowEquipment(EquipmentData data, Vector2 screenPosition)
    {
        if (data == null) { Hide(); return; }
        var t = Instance;
        t._text.text = data.GetTooltipText();
        t.LayoutToContent();
        t.UpdatePosition(screenPosition);
        t.SetActive(true);
    }

    public static void Move(Vector2 screenPosition)
    {
        var t = Instance;
        if (!t._visible) return;
        t.UpdatePosition(screenPosition);
    }

    public static void Hide()
    {
        if (_instance == null) return;
        _instance.SetActive(false);
    }

    void LayoutToContent()
    {
        if (_text == null || _root == null) return;
        // Compute preferred size with wrapping up to max width
        var content = _text.text;
        var preferred = _text.GetPreferredValues(content, _maxWidth, 0f);
        var textRT = _text.rectTransform;
        textRT.sizeDelta = new Vector2(Mathf.Min(preferred.x, _maxWidth), preferred.y);
        var panelSize = new Vector2(textRT.sizeDelta.x + _padding.x * 2f, textRT.sizeDelta.y + _padding.y * 2f);
        _root.sizeDelta = panelSize;
        textRT.anchoredPosition = new Vector2(_padding.x, -_padding.y);
    }

    void UpdatePosition(Vector2 screenPosition)
    {
        if (_root == null) return;
        var target = screenPosition + _offset;
        // Clamp to screen
        float w = Screen.width;
        float h = Screen.height;
        var size = _root.sizeDelta;
        target.x = Mathf.Clamp(target.x, 0f, w - size.x);
        // Because pivot is top-left, y decreases downward
        target.y = Mathf.Clamp(target.y, size.y, h);
        _root.anchoredPosition = new Vector2(target.x, - (h - target.y));
    }
}


