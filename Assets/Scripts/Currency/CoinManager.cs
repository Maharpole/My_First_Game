using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

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
        // Singleton pattern with DontDestroyOnLoad
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Subscribe to scene loaded event
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            // If we already have an instance, destroy this one
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from scene loaded event when destroyed
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Find and update the coin text UI in the new scene
        UpdateCoinTextReference();
    }

    private void UpdateCoinTextReference()
    {
        // Try to find the coin text in the new scene
        if (coinText == null)
        {
            // Look for any TextMeshProUGUI component with "coin" in its name
            TextMeshProUGUI[] texts = FindObjectsOfType<TextMeshProUGUI>();
            foreach (TextMeshProUGUI text in texts)
            {
                if (text.gameObject.name.ToLower().Contains("coin"))
                {
                    coinText = text;
                    break;
                }
            }
        }

        // Update the display with current coins
        UpdateCoinDisplay();
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