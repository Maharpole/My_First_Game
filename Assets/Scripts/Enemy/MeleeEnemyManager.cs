using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

public class MeleeEnemyManager : MonoBehaviour
{
    public static MeleeEnemyManager Instance { get; private set; }

    [System.Serializable]
    public class EnemyType
    {
        public string enemyName;
        public GameObject enemyPrefab;
        public int maxHealth = 10;
        public float moveSpeed = 5f;
        public int contactDamage = 10;
        public Color damageColor = Color.red;
        public float flashDuration = 0.1f;
        public ParticleSystem deathParticles;
        public int minCoins = 1;
        public int maxCoins = 3;
        public float dropChance = 1f;
        public float dropSpread = 1f;
    }

    [Header("Enemy Types")]
    public EnemyType[] enemyTypes;

    [Header("Spawn Settings")]
    [Tooltip("Maximum number of enemies that can exist at once")]
    public int maxEnemies = 10;

    [Tooltip("Time between spawn attempts in seconds")]
    public float spawnInterval = 5f;

    [Tooltip("Radius around spawner where enemies can spawn")]
    public float spawnRadius = 10f;

    [Header("Events")]
    public UnityEvent<GameObject> onEnemySpawned;
    public UnityEvent<GameObject> onEnemyDeath;

    private float nextSpawnTime;
    private List<GameObject> activeEnemies = new List<GameObject>();
    private Dictionary<GameObject, EnemyType> enemyTypeMap = new Dictionary<GameObject, EnemyType>();

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        nextSpawnTime = Time.time + spawnInterval;
    }

    private void Update()
    {
        // Check if it's time to spawn and we haven't reached max enemies
        if (Time.time >= nextSpawnTime && activeEnemies.Count < maxEnemies)
        {
            SpawnEnemy();
            nextSpawnTime = Time.time + spawnInterval;
        }

        // Update active enemies list
        activeEnemies.RemoveAll(enemy => enemy == null);
    }

    public void SpawnEnemy(int enemyTypeIndex = -1)
    {
        if (enemyTypes.Length == 0)
        {
            Debug.LogError("No enemy types configured!");
            return;
        }

        // If no specific type is requested, choose a random one
        if (enemyTypeIndex < 0 || enemyTypeIndex >= enemyTypes.Length)
        {
            enemyTypeIndex = Random.Range(0, enemyTypes.Length);
        }

        EnemyType enemyType = enemyTypes[enemyTypeIndex];

        // Calculate random position within spawn radius
        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPosition = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

        // Spawn the enemy
        GameObject newEnemy = Instantiate(enemyType.enemyPrefab, spawnPosition, Quaternion.identity);
        
        // Set up enemy components
        SetupEnemy(newEnemy, enemyType);

        // Add to active enemies list
        activeEnemies.Add(newEnemy);
        enemyTypeMap[newEnemy] = enemyType;

        // Trigger spawn event
        onEnemySpawned?.Invoke(newEnemy);
    }

    private void SetupEnemy(GameObject enemy, EnemyType enemyType)
    {
        // Set up EnemyHealth component
        EnemyHealth health = enemy.GetComponent<EnemyHealth>();
        if (health == null)
        {
            health = enemy.AddComponent<EnemyHealth>();
        }
        health.maxHealth = enemyType.maxHealth;
        health.currentHealth = enemyType.maxHealth;
        health.damageColor = enemyType.damageColor;
        health.flashDuration = enemyType.flashDuration;
        health.deathParticles = enemyType.deathParticles;
        health.onDeath.AddListener(() => OnEnemyDeath(enemy));

        // Set up EnemyController component
        EnemyController controller = enemy.GetComponent<EnemyController>();
        if (controller == null)
        {
            controller = enemy.AddComponent<EnemyController>();
        }
        controller.moveSpeed = enemyType.moveSpeed;

        // Set up CoinDropper component
        CoinDropper dropper = enemy.GetComponent<CoinDropper>();
        if (dropper == null)
        {
            dropper = enemy.AddComponent<CoinDropper>();
        }
        dropper.minCoins = enemyType.minCoins;
        dropper.maxCoins = enemyType.maxCoins;
        dropper.dropChance = enemyType.dropChance;
        dropper.dropSpread = enemyType.dropSpread;

        // Set up contact damage
        enemy.tag = "Enemy";
        if (enemy.GetComponent<Collider>() == null)
        {
            CapsuleCollider capsule = enemy.AddComponent<CapsuleCollider>();
            capsule.height = 2.0f;
            capsule.radius = 0.5f;
            capsule.center = new Vector3(0, 1.0f, 0);
        }
    }

    private void OnEnemyDeath(GameObject enemy)
    {
        // Remove from active enemies
        activeEnemies.Remove(enemy);
        enemyTypeMap.Remove(enemy);

        // Trigger death event
        onEnemyDeath?.Invoke(enemy);
    }

    // Visualize spawn radius in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
} 