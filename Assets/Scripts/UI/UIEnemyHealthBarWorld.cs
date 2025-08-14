using UnityEngine;
using UnityEngine.UI;

public class UIEnemyHealthBarWorld : MonoBehaviour
{
    [Header("Bindings")]
    [Tooltip("The enemy to track. If not set, will search in parents.")] public EnemyHealth target;
    [Tooltip("Image set to Filled (Horizontal) or Simple if using Width mode")] public Image healthFill;
    [Tooltip("Optional: world-space canvas to toggle visibility at full health")] public Canvas worldCanvas;

    public enum VisualMode { FillAmount, Width }
    [Header("Behavior")] public VisualMode visualMode = VisualMode.FillAmount;
    public Vector3 localOffset = new Vector3(0f, 1.6f, 0f);
    public bool faceCamera = true;
    public bool hideWhenFull = false;

    private float initialWidth = -1f;
    private Camera mainCamera;

    void Awake()
    {
        if (target == null) target = GetComponentInParent<EnemyHealth>();
        if (worldCanvas == null) worldCanvas = GetComponentInChildren<Canvas>();
        mainCamera = Camera.main;
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
        else
        {
            var rt = healthFill.rectTransform;
            if (initialWidth < 0f) initialWidth = rt.rect.width;
            // Shrink from left to right
            rt.pivot = new Vector2(0f, rt.pivot.y);
            var size = rt.sizeDelta;
            size.x = Mathf.Max(0f, initialWidth * t);
            rt.sizeDelta = size;
        }
    }

    void CacheInitialWidth()
    {
        if (healthFill == null) return;
        var rt = healthFill.rectTransform;
        initialWidth = rt.rect.width;
    }
}


