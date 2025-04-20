using UnityEngine;

public class CoinDropper : MonoBehaviour
{
    [Header("Coin Drop Settings")]
    [Tooltip("Minimum number of coins to drop")]
    public int minCoins = 1;

    [Tooltip("Maximum number of coins to drop")]
    public int maxCoins = 3;

    [Tooltip("Chance to drop coins (0-1)")]
    [Range(0f, 1f)]
    public float dropChance = 1f;

    [Tooltip("Random spread of coins when dropped")]
    public float dropSpread = 1f;

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
        // Check if we should drop coins
        if (Random.value <= dropChance)
        {
            // Calculate random number of coins to drop
            int coinsToDrop = Random.Range(minCoins, maxCoins + 1);
            
            // Update the CoinSpawner's settings temporarily
            CoinSpawner.Instance.coinsToDrop = coinsToDrop;
            CoinSpawner.Instance.dropSpread = dropSpread;
            
            // Spawn the coins
            CoinSpawner.Instance.SpawnCoins(transform.position);
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