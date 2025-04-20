using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthBar : MonoBehaviour
{
    [Header("Health Bar Settings")]
    [Tooltip("The health bar image component")]
    public Image healthBarImage;
    
    [Tooltip("The player health component")]
    public PlayerHealth playerHealth;
    
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
        
        if (playerHealth == null)
        {
            playerHealth = GetComponentInParent<PlayerHealth>();
            if (playerHealth == null)
            {
                Debug.LogError("No PlayerHealth component found!");
                return;
            }
        }
        
        // Subscribe to health changes
        playerHealth.onDamage.AddListener(UpdateHealthBar);
        
        // Set initial health
        UpdateHealthBar();
    }
    
    void UpdateHealthBar()
    {
        if (healthBarImage != null && playerHealth != null)
        {
            // Update the fill amount based on current health
            float healthPercent = (float)playerHealth.currentHealth / playerHealth.maxHealth;
            healthBarImage.fillAmount = healthPercent;
            
            // Update color based on health
            healthBarImage.color = Color.Lerp(damagedColor, healthyColor, healthPercent);
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (playerHealth != null)
        {
            playerHealth.onDamage.RemoveListener(UpdateHealthBar);
        }
    }
} 