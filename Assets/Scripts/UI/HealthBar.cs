using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [Header("Health Bar Settings")]
    [Tooltip("The health bar image component")]
    public Image healthBarImage;
    
    [Tooltip("The enemy this health bar belongs to")]
    public EnemyHealth enemyHealth;
    
    [Tooltip("Offset from the enemy's position")]
    public Vector3 offset = new Vector3(0, 2f, 0);
    
    private Camera mainCamera;
    
    void Start()
    {
        mainCamera = Camera.main;
        
        // Make sure we have the required components
        if (healthBarImage == null)
        {
            Debug.LogError("Health bar image not assigned!");
            return;
        }
        
        if (enemyHealth == null)
        {
            enemyHealth = GetComponentInParent<EnemyHealth>();
            if (enemyHealth == null)
            {
                Debug.LogError("No EnemyHealth component found in parent!");
                return;
            }
        }
        
        // Subscribe to health changes
        enemyHealth.onDamage.AddListener(UpdateHealthBar);
        
        // Set initial health
        UpdateHealthBar();
    }
    
    void Update()
    {
        // Make the health bar face the camera
        if (mainCamera != null)
        {
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                           mainCamera.transform.rotation * Vector3.up);
        }
        
        // Update position to follow enemy
        if (enemyHealth != null)
        {
            transform.position = enemyHealth.transform.position + offset;
        }
    }
    
    void UpdateHealthBar()
    {
        if (healthBarImage != null && enemyHealth != null)
        {
            // Update the fill amount based on current health
            healthBarImage.fillAmount = (float)enemyHealth.currentHealth / enemyHealth.maxHealth;
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (enemyHealth != null)
        {
            enemyHealth.onDamage.RemoveListener(UpdateHealthBar);
        }
    }
} 