using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Prefab of the enemy to spawn")]
    public GameObject enemyPrefab;

    [Tooltip("Maximum number of enemies that can exist at once")]
    public int maxEnemies = 10;

    [Tooltip("Time between spawn attempts in seconds")]
    public float spawnInterval = 5f;

    [Tooltip("Radius around spawner where enemies can spawn")]
    public float spawnRadius = 10f;

    [Header("Debug")]
    [Tooltip("Enable or disable debug logs")]
    public bool DebugLogs = false;

    [Tooltip("Current number of active enemies")]
    [SerializeField]
    private int currentEnemyCount = 0;

    private float nextSpawnTime;
    private List<GameObject> activeEnemies = new List<GameObject>();

    private void Start()
    {
        nextSpawnTime = Time.time + spawnInterval;
    }

    private void Update()
    {
        // Check if it's time to spawn and we haven't reached max enemies
        if (Time.time >= nextSpawnTime && currentEnemyCount < maxEnemies)
        {
            SpawnEnemy();
            nextSpawnTime = Time.time + spawnInterval;
        }

        // Update current enemy count by checking active enemies
        UpdateEnemyCount();
    }

    private void SpawnEnemy()
    {
        if (enemyPrefab == null)
        {
            if (DebugLogs)
            {
                Debug.LogError("Enemy prefab not assigned!");
            }
            return;
        }

        // Calculate random position within spawn radius
        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPosition = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

        // Spawn the enemy
        GameObject newEnemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        activeEnemies.Add(newEnemy);
        currentEnemyCount++;

        if (DebugLogs)
        {
            Debug.Log($"Spawned enemy. Current count: {currentEnemyCount}/{maxEnemies}");
        }
    }

    private void UpdateEnemyCount()
    {
        // Remove null references (destroyed enemies)
        activeEnemies.RemoveAll(enemy => enemy == null);
        
        // Update count
        currentEnemyCount = activeEnemies.Count;
    }

    // Visualize spawn radius in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}