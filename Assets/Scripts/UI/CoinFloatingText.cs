using UnityEngine;
using TMPro;

public class CoinFloatingText : MonoBehaviour
{
    [Header("Text Settings")]
    public TextMeshProUGUI textComponent;
    public float floatSpeed = 1f;
    public float fadeOutTime = 1f;
    public Color textColor = Color.yellow;
    
    private float currentTime = 0f;
    private Vector3 startPosition;
    private Vector3 targetPosition;

    private void Start()
    {
        if (textComponent == null)
        {
            textComponent = GetComponent<TextMeshProUGUI>();
        }
        
        startPosition = transform.position;
        targetPosition = startPosition + Vector3.up * 50f; // Move up 50 pixels
        textComponent.color = textColor;
    }

    private void Update()
    {
        currentTime += Time.deltaTime;
        
        // Move upward
        transform.position = Vector3.Lerp(startPosition, targetPosition, currentTime / fadeOutTime);
        
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