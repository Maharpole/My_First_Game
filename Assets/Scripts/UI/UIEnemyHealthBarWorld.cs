using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIEnemyHealthBarWorld : MonoBehaviour
{
    [Header("Bindings")]
    [Tooltip("The enemy to track. If not set, will search in parents.")] public EnemyHealth target;
    [Tooltip("Image set to Filled (Horizontal) or Simple if using Width mode")] public Image healthFill;
    [Tooltip("Optional: world-space canvas to toggle visibility at full health")] public Canvas worldCanvas;
    [Tooltip("Optional: Text to show enemy level next to the bar")] public TextMeshProUGUI levelText;
    [Tooltip("Optional: Background Image for the level box")] public Image levelBackground;
    [Tooltip("Optional: Text to show enemy name (as a TMP link)")] public TextMeshProUGUI enemyNameText;

    public enum VisualMode { FillAmount, Width, Height }
    [Header("Behavior")] public VisualMode visualMode = VisualMode.FillAmount;
    public Vector3 localOffset = new Vector3(0f, 1.6f, 0f);
    public bool faceCamera = true;
    public bool hideWhenFull = false;

    [Header("Level Box")]
    [Tooltip("Automatically size level background to text")] public bool autoSizeLevelBox = true;
    [Tooltip("Padding around level text (x = horizontal, y = vertical)")] public Vector2 levelPadding = new Vector2(6f, 3f);

    [Header("Name Link")]
    [Tooltip("Wrap the enemy name in a TMP <link> tag")] public bool useLinkForName = true;
    [Tooltip("Link ID to use for the enemy name link")] public string nameLinkId = "enemyName";
    [Tooltip("Override enemy name shown in UI (leave empty to auto from type)")] public string overrideEnemyName;

    [Header("High Level Indicator")]
    [Tooltip("If enemy level exceeds player's by this amount, show an icon instead of number")] public int highLevelDelta = 10;
    [Tooltip("Image used to display the high-level icon")] public Image levelIcon;
    [Tooltip("Sprite to show when enemy is far above player's level")] public Sprite highLevelSprite;

    private float initialWidth = -1f;
    private float initialHeight = -1f;
    private Camera mainCamera;
    private PlayerXP playerXP;

    void Awake()
    {
        if (target == null) target = GetComponentInParent<EnemyHealth>();
        if (worldCanvas == null) worldCanvas = GetComponentInChildren<Canvas>();
        mainCamera = Camera.main;
        var p = Player.Instance ?? Object.FindFirstObjectByType<Player>();
        if (p != null) playerXP = p.GetComponent<PlayerXP>();
        CacheInitialWidth();
    }

    void OnEnable()
    {
        if (target != null)
        {
            target.onDamage.AddListener(Refresh);
            target.onHeal.AddListener(Refresh);
            Refresh();
        }
    }

    void OnDisable()
    {
        if (target != null)
        {
            target.onDamage.RemoveListener(Refresh);
            target.onHeal.RemoveListener(Refresh);
        }
    }

    void LateUpdate()
    {
        if (target == null) return;
        // Position above target
        transform.position = target.transform.position + localOffset;
        if (faceCamera)
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera != null)
            {
                transform.rotation = Quaternion.LookRotation(mainCamera.transform.forward, mainCamera.transform.up);
            }
        }
    }

    public void Refresh()
    {
        if (target == null || healthFill == null || target.maxHealth <= 0) return;
        float t = Mathf.Clamp01((float)target.currentHealth / target.maxHealth);
        ApplyFill(t);
        UpdateLevelUI();
        UpdateNameUI();
        if (hideWhenFull && worldCanvas != null)
        {
            worldCanvas.enabled = t > 0f && t < 1f;
        }
    }

    void ApplyFill(float t)
    {
        if (visualMode == VisualMode.FillAmount)
        {
            healthFill.type = Image.Type.Filled;
            healthFill.fillMethod = Image.FillMethod.Horizontal;
            healthFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            healthFill.fillAmount = t;
        }
        else if (visualMode == VisualMode.Width)
        {
            var rt = healthFill.rectTransform;
            if (initialWidth < 0f) initialWidth = rt.rect.width;
            // Shrink from left to right
            rt.pivot = new Vector2(0f, rt.pivot.y);
            var size = rt.sizeDelta;
            size.x = Mathf.Max(0f, initialWidth * t);
            rt.sizeDelta = size;
        }
        else // VisualMode.Height
        {
            var rt = healthFill.rectTransform;
            if (initialHeight < 0f) initialHeight = rt.rect.height;
            // Shrink from bottom to top
            rt.pivot = new Vector2(rt.pivot.x, 0f);
            var size = rt.sizeDelta;
            size.y = Mathf.Max(0f, initialHeight * t);
            rt.sizeDelta = size;
        }
    }

    void CacheInitialWidth()
    {
        if (healthFill == null) return;
        var rt = healthFill.rectTransform;
        initialWidth = rt.rect.width;
        initialHeight = rt.rect.height;
    }

    void UpdateLevelUI()
    {
        if (target == null) return;
        if (playerXP == null)
        {
            var p = Player.Instance ?? Object.FindFirstObjectByType<Player>();
            if (p != null) playerXP = p.GetComponent<PlayerXP>();
        }

        int playerLevel = playerXP != null ? playerXP.level : 1;
        bool showIcon = levelIcon != null && highLevelSprite != null && (target.level - playerLevel) >= highLevelDelta;

        if (showIcon)
        {
            if (levelText != null) levelText.enabled = false;
            levelIcon.enabled = true;
            if (levelIcon.sprite != highLevelSprite) levelIcon.sprite = highLevelSprite;
            levelIcon.SetNativeSize();
            if (autoSizeLevelBox && levelBackground != null)
            {
                var irt = levelIcon.rectTransform;
                var sizeIcon = irt.rect.size;
                var brt = levelBackground.rectTransform;
                var size = brt.sizeDelta;
                size.x = sizeIcon.x + levelPadding.x * 2f;
                size.y = sizeIcon.y + levelPadding.y * 2f;
                brt.sizeDelta = size;
            }
        }
        else
        {
            if (levelIcon != null) levelIcon.enabled = false;
            if (levelText != null)
            {
                levelText.enabled = true;
                levelText.text = target.level.ToString();
                if (autoSizeLevelBox && levelBackground != null)
                {
                    // Size background to fit text with padding
                    levelText.ForceMeshUpdate();
                    float w = levelText.preferredWidth + levelPadding.x * 2f;
                    float h = levelText.preferredHeight + levelPadding.y * 2f;
                    var rt = levelBackground.rectTransform;
                    var size = rt.sizeDelta;
                    size.x = w; size.y = h;
                    rt.sizeDelta = size;
                }
            }
        }
    }

    void UpdateNameUI()
    {
        if (enemyNameText == null || target == null) return;
        string display = "";
        // Inspector override has highest priority
        if (!string.IsNullOrEmpty(overrideEnemyName))
        {
            display = overrideEnemyName;
        }
        else
        {
            var identity = target.GetComponent<EnemyIdentity>();
            if (identity != null)
            {
                display = identity.GetDisplayName();
            }
            else
            {
                // Fallback to GameObject name
                display = target.gameObject != null ? target.gameObject.name : "";
            }
        }
        if (useLinkForName)
        {
            enemyNameText.text = $"<link=\"{nameLinkId}\">{display}</link>";
        }
        else
        {
            enemyNameText.text = display;
        }
    }
}


