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
    private bool _extendedView;
    private EquipmentData _currentData;
    private static bool _globalExtendedView;

    public static void SetExtendedView(bool enabled)
    {
        _globalExtendedView = enabled;
        var t = _instance;
        if (t != null)
        {
            t._extendedView = enabled;
            if (t._visible && t._currentData != null)
            {
                t._text.text = enabled ? UITooltipExtensions.BuildExtendedTooltip(t._currentData) : t._currentData.GetTooltipText();
                t.LayoutToContent();
            }
        }
    }
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
        _text.fontSize = 15f; // body font size (matches normal tooltip)
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
        bool ext = _globalExtendedView;
        t._extendedView = ext;
        t._currentData = data;
        // Title color by rarity and body content
        string body = ext ? UITooltipExtensions.BuildExtendedTooltip(data) : data.GetTooltipText();
        t.ApplyWrapping();
        t._text.text = body;
        t.LayoutToContent();
        t.UpdatePosition(screenPosition);
        t.SetActive(true);
    }

    void ApplyWrapping()
    {
        if (_text == null) return;
        // Keep each modifier on one line by disabling word wrapping and allowing wider tooltips for extended view
        _text.textWrappingMode = TextWrappingModes.NoWrap;
        _maxWidth = 4020f;
    }

    public static void Move(Vector2 screenPosition)
    {
        var t = Instance;
        if (!t._visible) return;
        // Rebuild text if extended toggle changes while hovering
        bool ext = _globalExtendedView;
        if (ext != t._extendedView)
        {
            t._extendedView = ext;
            if (t._currentData != null)
            {
                string body = ext ? UITooltipExtensions.BuildExtendedTooltip(t._currentData) : t._currentData.GetTooltipText();
                t.ApplyWrapping();
                t._text.text = body;
                t.LayoutToContent();
            }
        }
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

static class UITooltipExtensions
{
    public static string BuildExtendedTooltip(EquipmentData data)
    {
        if (data == null) return "";
        // Compose extended view with tier info if available via EquipmentData API
        // We use reflection to try to get generatedAffixes list without breaking encapsulation
        var fi = typeof(EquipmentData).GetField("generatedAffixes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        var nameColor = data.GetTooltipRarityColor();
        sb.AppendLine($"<size=30><b><color=#{ColorUtility.ToHtmlStringRGB(nameColor)}>{data.equipmentName}</color></b></size>");
        sb.AppendLine($"{data.equipmentType} (Level {data.requiredLevel})");
        sb.AppendLine();
        // Base first
        if (data.baseStats != null)
        {
            for (int i = 0; i < data.baseStats.Count; i++)
            {
                var st = data.baseStats[i];
                if (!System.Enum.IsDefined(typeof(StatType), (int)st.statType)) continue;
                sb.AppendLine(StatTypeInfo.ToDisplayString(st));
            }
        }
        // Separator
        bool hasBase = data.baseStats != null && data.baseStats.Count > 0;
        var genList = fi != null ? fi.GetValue(data) as System.Collections.IList : null;
        bool hasGen = genList != null && genList.Count > 0;
        if (hasBase && hasGen) sb.AppendLine("<color=#888888>────────────</color>");
        // Random with tier info
        if (hasGen)
        {
            for (int i = 0; i < genList.Count; i++)
            {
                var ga = genList[i];
                var statType = (StatType)ga.GetType().GetField("statType").GetValue(ga);
                float value = (float)ga.GetType().GetField("value").GetValue(ga);
                float tmin = (float)ga.GetType().GetField("tierMin").GetValue(ga);
                float tmax = (float)ga.GetType().GetField("tierMax").GetValue(ga);
                string tname = (string)ga.GetType().GetField("tierName").GetValue(ga);
                string aname = (string)ga.GetType().GetField("displayName").GetValue(ga);
                bool isPct = StatTypeInfo.EffectiveIsPercent(statType, false);
                string valStr = isPct ? $"+{value}%" : $"+{value}";
                string rangeStr = isPct ? $" ({Mathf.RoundToInt(tmin)}%–{Mathf.RoundToInt(tmax)}%)" : $" ({Mathf.RoundToInt(tmin)}–{Mathf.RoundToInt(tmax)})";
                string label = StatTypeInfo.GetDisplayLabel(statType);
                string inc = isPct ? " increased" : string.Empty;
                // Inline range next to rolled value, then smaller parentheses for tier and affix name
                sb.AppendLine($"{valStr}{rangeStr}{inc} {label} <size=80%><color=#7FA7FF>({tname}, {aname})</color></size>");
            }
        }
        if (!string.IsNullOrEmpty(data.description))
        {
            sb.AppendLine();
            sb.AppendLine(data.description);
        }
        return sb.ToString();
    }
}



