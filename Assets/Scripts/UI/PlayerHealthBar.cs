using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthBar : MonoBehaviour
{
    [Header("Health Bar Settings")]
    [Tooltip("The health bar image component")]
    public Image healthBarImage;
    
    [Header("Visual Effects")]
    [Tooltip("Color when health is high")]
    public Color healthyColor = Color.green;
    
    [Tooltip("Color when health is low")]
    public Color damagedColor = Color.red;
    
    [Tooltip("How fast the health bar color changes")]
    public float colorChangeSpeed = 5f;
    
    private void Start()
    {
        // Make sure we have the required components
        if (healthBarImage == null)
        {
            Debug.LogError("Health bar image not assigned!");
            return;
        }
        
        // Subscribe to health changes
        PlayerDataManager.Instance.onHealthChanged.AddListener(UpdateHealthBar);
        PlayerDataManager.Instance.onDamage.AddListener(() => UpdateHealthBar(PlayerDataManager.Instance.currentHealth));
        
        // Set initial health
        UpdateHealthBar(PlayerDataManager.Instance.currentHealth);
    }
    
    void UpdateHealthBar(int currentHealth)
    {
        if (healthBarImage != null)
        {
            // Update the fill amount based on current health
            float healthPercent = (float)currentHealth / PlayerDataManager.Instance.maxHealth;
            healthBarImage.fillAmount = healthPercent;
            
            // Update color based on health
            healthBarImage.color = Color.Lerp(damagedColor, healthyColor, healthPercent);
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.onHealthChanged.RemoveListener(UpdateHealthBar);
            PlayerDataManager.Instance.onDamage.RemoveListener(() => UpdateHealthBar(PlayerDataManager.Instance.currentHealth));
        }
    }
} 