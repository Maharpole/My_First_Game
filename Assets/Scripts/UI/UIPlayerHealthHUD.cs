using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIPlayerHealthHUD : MonoBehaviour
{
    [Header("Bindings")]
    [Tooltip("Image set to Filled (Horizontal) that represents current health")] public Image healthFill;
    [Tooltip("Text that shows 'current / max'")] public TextMeshProUGUI healthText;

    [Header("Bindings - XP (optional)")]
    public Slider xpBar;
    public TextMeshProUGUI xpText;
    public TextMeshProUGUI levelText;

    // Color remains constant; choose how the bar shrinks
    public enum VisualMode { FillAmount, Width, Height }
    [Header("Behavior")] public VisualMode visualMode = VisualMode.Width;

    private Player player;
    private PlayerXP playerXP;
    private float initialWidth = -1f;
    private float initialHeight = -1f;

    void Awake()
    {
        if (player == null) player = Player.Instance ?? Object.FindFirstObjectByType<Player>();
        CacheInitialWidth();
        if (playerXP == null && player != null) playerXP = player.GetComponent<PlayerXP>();
    }

    void OnEnable()
    {
        if (player == null) player = Player.Instance ?? Object.FindFirstObjectByType<Player>();
        if (playerXP == null && player != null) playerXP = player.GetComponent<PlayerXP>();
        if (player != null)
        {
            player.onHealthChanged.AddListener(OnHealthChanged);
            // Initialize
            OnHealthChanged(player.CurrentHealth);
        }
        if (playerXP != null)
        {
            playerXP.onXPChanged.AddListener(OnXPChanged);
            playerXP.onLevelChanged.AddListener(OnLevelChanged);
            OnXPChanged(playerXP.currentXP, Mathf.Max(1, playerXP.xpToNextLevel));
            OnLevelChanged(playerXP.level);
        }
    }

    void OnDisable()
    {
        if (player != null)
        {
            player.onHealthChanged.RemoveListener(OnHealthChanged);
        }
        if (playerXP != null)
        {
            playerXP.onXPChanged.RemoveListener(OnXPChanged);
            playerXP.onLevelChanged.RemoveListener(OnLevelChanged);
        }
    }

    void OnHealthChanged(int current)
    {
        if (player == null) return;
        int max = Mathf.Max(1, player.MaxHealth);
        float t = Mathf.Clamp01((float)current / max);

        if (healthFill != null)
        {
            if (visualMode == VisualMode.FillAmount)
            {
                healthFill.type = Image.Type.Filled; // ensure mode
                healthFill.fillMethod = Image.FillMethod.Horizontal;
                healthFill.fillOrigin = (int)Image.OriginHorizontal.Left;
                healthFill.fillAmount = t;
            }
            else if (visualMode == VisualMode.Width)
            {
                var rt = healthFill.rectTransform;
                if (initialWidth < 0f) initialWidth = rt.rect.width;
                // Ensure pivot on left so width reduction is from right to left
                rt.pivot = new Vector2(0f, rt.pivot.y);
                // Preserve height
                var size = rt.sizeDelta;
                // If anchors are stretched, prefer setting a LayoutElement instead; here we assume non-stretched width
                size.x = Mathf.Max(0f, initialWidth * t);
                rt.sizeDelta = size;
            }
            else // VisualMode.Height
            {
                var rt = healthFill.rectTransform;
                if (initialHeight < 0f) initialHeight = rt.rect.height;
                // Ensure pivot on bottom so height reduction is bottom->top
                rt.pivot = new Vector2(rt.pivot.x, 0f);
                var size = rt.sizeDelta;
                size.y = Mathf.Max(0f, initialHeight * t);
                rt.sizeDelta = size;
            }
        }

        if (healthText != null)
        {
            healthText.text = $"{current} / {max}";
        }
    }

    void CacheInitialWidth()
    {
        if (healthFill == null) return;
        var rt = healthFill.rectTransform;
        initialWidth = rt.rect.width;
        initialHeight = rt.rect.height;
    }

    // === XP UI ===
    void OnXPChanged(int current, int toNext)
    {
        if (xpBar != null)
        {
            float t = Mathf.Clamp01(toNext > 0 ? (float)current / toNext : 0f);
            xpBar.minValue = 0f; xpBar.maxValue = 1f; xpBar.value = t;
        }
        if (xpText != null)
        {
            xpText.text = $"XP: {current} / {toNext}";
        }
    }

    void OnLevelChanged(int level)
    {
        if (levelText != null) levelText.text = $"Lv {level}";
    }
}


