using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class WeaponUpgradeUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject upgradePanel;
    public Transform weaponButtonContainer;
    public Transform upgradeButtonContainer;
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI activeWeaponsText;
    
    [Header("Prefabs")]
    public GameObject weaponButtonPrefab;
    public GameObject upgradeButtonPrefab;
    
    // Deprecated
    
    void Start()
    {
        // Disabled
        
        // Subscribe to events
        if (Player.Instance != null)
        {
            Player.Instance.onCoinsChanged.AddListener(UpdateCoinsDisplay);
        }
        // Disabled
        
        // Initialize UI
        // Disabled
        if (Player.Instance != null)
        {
            UpdateCoinsDisplay(Player.Instance.Coins);
        }
        // Disabled
        
        // Hide upgrade panel initially
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(false);
        }
    }
    
    void InitializeWeaponButtons()
    {
        return;
    }
    
    void SelectWeapon(int index)
    {
        return;
    }
    
    void ShowUpgrades(WeaponData weapon)
    {
        if (upgradeButtonContainer == null || upgradeButtonPrefab == null) return;
        
        // Clear existing upgrade buttons
        foreach (Transform child in upgradeButtonContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Disabled
        
        // Show the upgrade panel
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(true);
        }
    }
    
    void PurchaseUpgrade(WeaponData weapon, WeaponData.WeaponUpgrade upgrade)
    {
        return;
    }
    
    void UpdateCoinsDisplay(int coins)
    {
        if (coinsText != null)
        {
            coinsText.text = $"Coins: {coins}";
        }
    }
    
    void OnWeaponAdded(WeaponData weapon)
    {
        UpdateActiveWeaponsDisplay();
    }
    
    void UpdateActiveWeaponsDisplay()
    {
        if (activeWeaponsText == null) return;
        
        activeWeaponsText.text = string.Empty;
    }
    
    public void CloseUpgradePanel()
    {
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(false);
        }
    }
    
    public void PurchaseWeapon(int index)
    {
        return;
    }
} 