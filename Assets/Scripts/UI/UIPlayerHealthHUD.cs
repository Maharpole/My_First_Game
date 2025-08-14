using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIPlayerHealthHUD : MonoBehaviour
{
    [Header("Bindings")]
    [Tooltip("Image set to Filled (Horizontal) that represents current health")] public Image healthFill;
    [Tooltip("Text that shows 'current / max'")] public TextMeshProUGUI healthText;

    // Color remains constant; choose how the bar shrinks
    public enum VisualMode { FillAmount, Width }
    [Header("Behavior")] public VisualMode visualMode = VisualMode.Width;

    private Player player;
    private float initialWidth = -1f;

    void Awake()
    {
        if (player == null) player = Player.Instance ?? FindObjectOfType<Player>();
        CacheInitialWidth();
    }

    void OnEnable()
    {
        if (player == null) player = Player.Instance ?? FindObjectOfType<Player>();
        if (player != null)
        {
            player.onHealthChanged.AddListener(OnHealthChanged);
            // Initialize
            OnHealthChanged(player.CurrentHealth);
        }
    }

    void OnDisable()
    {
        if (player != null)
        {
            player.onHealthChanged.RemoveListener(OnHealthChanged);
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
            else // VisualMode.Width
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
    }
}


