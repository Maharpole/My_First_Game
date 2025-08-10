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
    
    private WeaponManager weaponManager;
    private WeaponData selectedWeapon;
    
    void Start()
    {
        weaponManager = FindObjectOfType<WeaponManager>();
        if (weaponManager == null)
        {
            Debug.LogError("WeaponManager not found in the scene!");
            return;
        }
        
        // Subscribe to events
        if (Player.Instance != null)
        {
            Player.Instance.onCoinsChanged.AddListener(UpdateCoinsDisplay);
        }
        weaponManager.onWeaponAdded.AddListener(OnWeaponAdded);
        
        // Initialize UI
        InitializeWeaponButtons();
        if (Player.Instance != null)
        {
            UpdateCoinsDisplay(Player.Instance.Coins);
        }
        UpdateActiveWeaponsDisplay();
        
        // Hide upgrade panel initially
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(false);
        }
    }
    
    void InitializeWeaponButtons()
    {
        if (weaponButtonContainer == null || weaponButtonPrefab == null) return;
        
        // Clear existing buttons
        foreach (Transform child in weaponButtonContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Create buttons for each weapon
        for (int i = 0; i < weaponManager.availableWeapons.Length; i++)
        {
            WeaponData weapon = weaponManager.availableWeapons[i];
            GameObject buttonObj = Instantiate(weaponButtonPrefab, weaponButtonContainer);
            
            // Set button text
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = weapon.weaponName;
            }
            
            // Set button icon if available
            Image buttonImage = buttonObj.GetComponent<Image>();
            if (buttonImage != null && weapon.weaponIcon != null)
            {
                buttonImage.sprite = weapon.weaponIcon;
            }
            
            // Add click listener
            Button button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                int index = i; // Capture the index for the lambda
                button.onClick.AddListener(() => SelectWeapon(index));
            }
        }
    }
    
    void SelectWeapon(int index)
    {
        selectedWeapon = weaponManager.availableWeapons[index];
        ShowUpgrades(selectedWeapon);
    }
    
    void ShowUpgrades(WeaponData weapon)
    {
        if (upgradeButtonContainer == null || upgradeButtonPrefab == null) return;
        
        // Clear existing upgrade buttons
        foreach (Transform child in upgradeButtonContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Create buttons for each available upgrade
        foreach (var upgrade in weapon.availableUpgrades)
        {
            // Skip if already purchased
            if (weaponManager.GetPurchasedUpgrades(weapon).Contains(upgrade))
                continue;
                
            GameObject buttonObj = Instantiate(upgradeButtonPrefab, upgradeButtonContainer);
            
            // Set upgrade info
            TextMeshProUGUI[] texts = buttonObj.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length >= 2)
            {
                texts[0].text = upgrade.upgradeName;
                texts[1].text = $"{upgrade.description}\nCost: {upgrade.cost} coins";
            }
            
            // Add click listener
            Button button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => PurchaseUpgrade(weapon, upgrade));
                
                // Disable button if can't afford
                button.interactable = Player.Instance != null && Player.Instance.CanAffordUpgrade(upgrade);
            }
        }
        
        // Show the upgrade panel
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(true);
        }
    }
    
    void PurchaseUpgrade(WeaponData weapon, WeaponData.WeaponUpgrade upgrade)
    {
        if (weaponManager.PurchaseUpgrade(weapon, upgrade))
        {
            // Refresh the upgrades display
            ShowUpgrades(weapon);
            if (Player.Instance != null)
            {
                UpdateCoinsDisplay(Player.Instance.Coins);
            }
        }
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
        
        List<WeaponData> activeWeapons = weaponManager.GetActiveWeapons();
        
        if (activeWeapons.Count == 0)
        {
            activeWeaponsText.text = "No weapons active";
        }
        else
        {
            string weaponsList = "Active Weapons:\n";
            foreach (var weapon in activeWeapons)
            {
                weaponsList += $"- {weapon.weaponName}\n";
            }
            activeWeaponsText.text = weaponsList;
        }
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
        weaponManager.AddWeapon(index);
        UpdateActiveWeaponsDisplay();
    }
} 