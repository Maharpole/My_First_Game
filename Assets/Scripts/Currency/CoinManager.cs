using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("Text component to display coin count")]
    public TextMeshProUGUI coinText;

    [Header("Coin Settings")]
    [Tooltip("Number of coins to add when collecting a coin")]
    public int coinsPerPickup = 1;

    private int currentCoins = 0;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        UpdateCoinDisplay();
    }

    public void AddCoins(int amount)
    {
        currentCoins += amount;
        UpdateCoinDisplay();
    }

    public bool SpendCoins(int amount)
    {
        if (currentCoins >= amount)
        {
            currentCoins -= amount;
            UpdateCoinDisplay();
            return true;
        }
        return false;
    }

    private void UpdateCoinDisplay()
    {
        if (coinText != null)
        {
            coinText.text = $"Coins: {currentCoins}";
        }
    }

    public int GetCurrentCoins()
    {
        return currentCoins;
    }
} 