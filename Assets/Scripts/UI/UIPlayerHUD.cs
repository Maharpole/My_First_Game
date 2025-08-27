using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class UIPlayerHUD : MonoBehaviour
{
    private static UIPlayerHUD _instance;
    [Header("Bindings - Health (optional if you already use UIPlayerHealthHUD)")]
    public UIPlayerHealthHUD healthHUD;

    // XP bindings moved to UIPlayerHealthHUD for screen-space UI

    [Header("Bindings - Dash (squares + recharge line)")]
    public Transform dashSquaresContainer; // parent with child Images representing charges
    public GameObject dashSquareTemplate;  // optional: template square (inactive child or prefab)
    public Color dashAvailableColor = Color.white;
    public Color dashSpentColor = Color.gray;
    public Image dashRechargeLine; // thin Image under squares, fillAmount/width shows progress
    public TextMeshProUGUI dashText; // optional text fallback

    [Header("Rendering")]
    public Material alwaysOnTopMaterial; // Optional: assign material with ZTest Always

    private Player player;
    private PlayerXP playerXP;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        if (player == null) player = Player.Instance ?? Object.FindFirstObjectByType<Player>();
        if (playerXP == null) playerXP = player != null ? player.GetComponent<PlayerXP>() : null;
    }

    void OnEnable()
    {
        if (player == null) player = Player.Instance ?? Object.FindFirstObjectByType<Player>();
        if (playerXP == null && player != null) playerXP = player.GetComponent<PlayerXP>();

        // Ensure always-on-top material is applied to UI graphics if provided
        if (alwaysOnTopMaterial != null)
        {
            ApplyAlwaysOnTopMaterial();
        }

        // Dash
        if (player != null)
        {
            player.onDashChargesChanged.AddListener(OnDashChanged);
            OnDashChanged(player.CurrentDashCharges, player.maxDashCharges);
        }

        // XP moved; no setup here
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    void ApplyAlwaysOnTopMaterial()
    {
        var graphics = GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] != null) graphics[i].material = alwaysOnTopMaterial;
        }
    }

    void OnDisable()
    {
        if (player != null)
        {
            player.onDashChargesChanged.RemoveListener(OnDashChanged);
        }
        // XP moved; no teardown here
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Resolve references and refresh once per scene load
        player = Player.Instance ?? Object.FindFirstObjectByType<Player>();
        playerXP = player != null ? player.GetComponent<PlayerXP>() : null;
        if (player != null)
        {
            OnDashChanged(player.CurrentDashCharges, player.maxDashCharges);
        }
        // XP moved; nothing to refresh here
        if (alwaysOnTopMaterial != null) ApplyAlwaysOnTopMaterial();
    }

    // XP moved; handlers removed

    void OnDashChanged(int current, int max)
    {
        EnsureDashSquaresCount(Mathf.Max(0, max));
        // Color squares
        if (dashSquaresContainer != null)
        {
            int childCount = dashSquaresContainer.childCount;
            for (int i = 0; i < childCount; i++)
            {
                var img = dashSquaresContainer.GetChild(i)?.GetComponent<Image>();
                if (img == null) continue;
                img.color = (i < current) ? dashAvailableColor : dashSpentColor;
            }
        }
        // Optional text
        if (dashText != null) dashText.text = $"{current}/{Mathf.Max(1, max)}";
        // Recharge line updates in Update
    }

    void EnsureDashSquaresCount(int required)
    {
        if (dashSquaresContainer == null) return;
        int current = dashSquaresContainer.childCount;
        // Grow
        if (current < required)
        {
            // Choose template: explicit template > first existing child (if any) > new Image
            GameObject template = dashSquareTemplate;
            if (template == null && current > 0)
            {
                template = dashSquaresContainer.GetChild(0).gameObject;
            }
            for (int i = current; i < required; i++)
            {
                GameObject go;
                if (template != null)
                {
                    go = Instantiate(template, dashSquaresContainer);
                    go.SetActive(true);
                }
                else
                {
                    go = new GameObject("DashSquare", typeof(RectTransform), typeof(Image));
                    var rt = go.GetComponent<RectTransform>();
                    rt.SetParent(dashSquaresContainer, false);
                    rt.sizeDelta = new Vector2(16f, 16f);
                    var img = go.GetComponent<Image>();
                    img.color = dashSpentColor;
                }
            }
        }
        // Shrink
        else if (current > required)
        {
            for (int i = current - 1; i >= required; i--)
            {
                var child = dashSquaresContainer.GetChild(i);
                if (child != null) Destroy(child.gameObject);
            }
        }
    }

    void Update()
    {
        if (dashRechargeLine == null || player == null) return;
        float t = player.DashRechargeProgress01;
        // Handle both Filled image and width-based images
        if (dashRechargeLine.type == Image.Type.Filled)
        {
            dashRechargeLine.fillMethod = Image.FillMethod.Horizontal;
            dashRechargeLine.fillOrigin = (int)Image.OriginHorizontal.Left;
            dashRechargeLine.fillAmount = t;
        }
        else
        {
            var rt = dashRechargeLine.rectTransform;
            var size = rt.sizeDelta;
            // assume parent width is desired full length
            float full = rt.parent != null ? (rt.parent as RectTransform).rect.width : size.x;
            size.x = Mathf.Max(0f, full * t);
            rt.sizeDelta = size;
            rt.pivot = new Vector2(0f, rt.pivot.y);
        }
    }
}


