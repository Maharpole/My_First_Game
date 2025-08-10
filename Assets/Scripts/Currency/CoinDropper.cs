using UnityEngine;

public class CoinDropper : MonoBehaviour
{
    [Header("Coin Drop Settings")]
    [Tooltip("Minimum number of coins to drop")] public int minCoins = 1;
    [Tooltip("Maximum number of coins to drop")] public int maxCoins = 3;
    [Tooltip("Chance to drop coins (0-1)")]
    [Range(0f, 1f)] public float dropChance = 1f;
    [Tooltip("Random spread of coins when dropped")] public float dropSpread = 1f;

    [Header("Prefab")]
    [Tooltip("Coin prefab to spawn on drop")] public GameObject coinPrefab;

    private void Start()
    {
        // Get the EnemyHealth component
        EnemyHealth enemyHealth = GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            // Subscribe to the death event
            enemyHealth.onDeath.AddListener(OnEnemyDeath);
        }
        else
        {
            Debug.LogError("CoinDropper requires an EnemyHealth component!");
        }
    }

    private void OnEnemyDeath()
    {
        if (coinPrefab == null) return;
        // Check if we should drop coins
        if (Random.value > dropChance) return;

        int coinsToSpawn = Random.Range(minCoins, maxCoins + 1);
        for (int i = 0; i < coinsToSpawn; i++)
        {
            Vector3 offset = new Vector3(Random.Range(-dropSpread, dropSpread), 0.5f, Random.Range(-dropSpread, dropSpread));
            Instantiate(coinPrefab, transform.position + offset, Quaternion.identity);
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        EnemyHealth enemyHealth = GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.onDeath.RemoveListener(OnEnemyDeath);
        }
    }
} 