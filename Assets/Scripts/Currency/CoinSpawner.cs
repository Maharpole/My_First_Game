using UnityEngine;

public class CoinSpawner : MonoBehaviour
{
    public static CoinSpawner Instance { get; private set; }

    [Header("Coin Settings")]
    [Tooltip("Coin prefab to spawn")]
    public GameObject coinPrefab;

    [Tooltip("Number of coins to spawn")]
    public int coinsToDrop = 1;

    [Tooltip("Random spread of coins when dropped")]
    public float dropSpread = 1f;

    [Header("Coin Rotation")]
    [Tooltip("Rotate around X axis")]
    public bool rotateX = true;

    [Tooltip("Rotate around Y axis")]
    public bool rotateY = false;

    [Tooltip("Rotate around Z axis")]
    public bool rotateZ = false;

    [Tooltip("Rotation amount in degrees")]
    public float rotationAmount = 90f;

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

    public void SpawnCoins(Vector3 position)
    {
        if (coinPrefab == null)
        {
            Debug.LogError("Coin prefab not assigned!");
            return;
        }

        for (int i = 0; i < coinsToDrop; i++)
        {
            Vector3 randomOffset = new Vector3(
                Random.Range(-dropSpread, dropSpread),
                0.5f, // Slightly above ground
                Random.Range(-dropSpread, dropSpread)
            );
            
            // Calculate rotation based on selected axes
            float xRotation = rotateX ? rotationAmount : 0f;
            float yRotation = rotateY ? rotationAmount : 0f;
            float zRotation = rotateZ ? rotationAmount : 0f;
            
            Quaternion coinRotation = Quaternion.Euler(xRotation, yRotation, zRotation);
            Instantiate(coinPrefab, position + randomOffset, coinRotation);
        }
    }
} 