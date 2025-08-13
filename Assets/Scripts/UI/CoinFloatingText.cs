using UnityEngine;
using TMPro;

public class CoinFloatingText : MonoBehaviour
{
    [Header("Text Settings")]
    public TextMeshProUGUI textComponent;
    public float floatSpeed = 1f;
    public float fadeOutTime = 1f;
    public Color textColor = Color.yellow;
    [Tooltip("Pixel offset upward from the spawn point")] public float verticalOffset = 50f;
    
    private float currentTime = 0f;
    private RectTransform rectTransform;
    private Vector2 startAnchored;
    private Vector2 targetAnchored;

    private void Start()
    {
        if (textComponent == null)
        {
            textComponent = GetComponent<TextMeshProUGUI>();
        }
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            startAnchored = rectTransform.anchoredPosition;
            targetAnchored = startAnchored + new Vector2(0f, verticalOffset);
        }
        textComponent.color = textColor;
    }

    private void Update()
    {
        currentTime += Time.deltaTime;
        
        // Move upward in UI space
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = Vector2.Lerp(startAnchored, targetAnchored, currentTime / fadeOutTime);
        }
        
        // Fade out
        float alpha = Mathf.Lerp(1f, 0f, currentTime / fadeOutTime);
        Color color = textComponent.color;
        color.a = alpha;
        textComponent.color = color;
        
        // Destroy when fully faded
        if (currentTime >= fadeOutTime)
        {
            Destroy(gameObject);
        }
    }

    public void SetAmount(int amount)
    {
        if (textComponent != null)
        {
            textComponent.text = $"+{amount}";
        }
    }
} 