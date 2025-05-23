using UnityEngine;

public class Coin : MonoBehaviour
{
    [Header("Coin Settings")]
    [Tooltip("Value of this coin")]
    public int value = 1;

    [Header("UI")]
    [Tooltip("Prefab for the floating text that appears when coin is collected")]
    public GameObject floatingTextPrefab;

    [Tooltip("Rotation speed in degrees per second")]
    public float rotationSpeed = 90f;

    [Tooltip("Float height in units")]
    public float floatHeight = 0.5f;

    [Tooltip("Float speed in cycles per second")]
    public float floatSpeed = 1f;

    [Header("Magnet Settings")]
    [Tooltip("Distance at which coins start moving toward player")]
    public float magnetRange = 5f;

    [Tooltip("Speed at which coins move toward player")]
    public float magnetSpeed = 10f;

    [Tooltip("Acceleration of coin movement")]
    public float magnetAcceleration = 2f;

    [Header("Visual Effects")]
    [Tooltip("Material to use when coin is magnetized")]
    public Material magnetizedMaterial;

    [Header("Sound Effects")]
    [Tooltip("Sound played when coin is collected")]
    public AudioClip collectSound;

    [Tooltip("Volume of the collect sound")]
    [Range(0f, 1f)]
    public float collectVolume = 1f;

    private Vector3 startPosition;
    private float timeOffset;
    private float currentSpeed = 0f;
    private bool isMagnetized = false;
    private Transform playerTransform;
    private SphereCollider coinCollider;
    private bool isCollected = false;
    private Collider playerCollider;
    private Material originalMaterial;
    private MeshRenderer meshRenderer;

    private void Start()
    {
        startPosition = transform.position;
        timeOffset = Random.Range(0f, 2f * Mathf.PI);
        
        // Set up collider
        coinCollider = GetComponent<SphereCollider>();
        if (coinCollider == null)
        {
            coinCollider = gameObject.AddComponent<SphereCollider>();
        }
        coinCollider.isTrigger = false; // Changed to non-trigger for overlap detection
        coinCollider.radius = 0.5f;
        
        // Get mesh renderer and store original material
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            originalMaterial = meshRenderer.material;
        }
        
        // Find the player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerCollider = player.GetComponent<Collider>();
            if (playerCollider == null)
            {
                Debug.LogError("Player has no collider!");
            }
            Debug.Log("Player found successfully");
        }
        else
        {
            Debug.LogError("Player not found! Make sure your player has the 'Player' tag.");
        }
    }

    private void Update()
    {
        if (playerTransform == null || isCollected) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // Check if coin should be magnetized
        if (distanceToPlayer <= magnetRange)
        {
            if (!isMagnetized)
            {
                isMagnetized = true;
                // Change material when magnetized
                if (meshRenderer != null && magnetizedMaterial != null)
                {
                    meshRenderer.material = magnetizedMaterial;
                }
            }
            
            // Accelerate toward player
            currentSpeed = Mathf.Min(currentSpeed + magnetAcceleration * Time.deltaTime, magnetSpeed);
            
            // Move toward player
            Vector3 direction = (playerTransform.position - transform.position).normalized;
            transform.position += direction * currentSpeed * Time.deltaTime;

            // Check for overlap with player
            if (playerCollider != null && coinCollider != null)
            {
                if (coinCollider.bounds.Intersects(playerCollider.bounds))
                {
                    CollectCoin();
                }
            }
        }
        else if (isMagnetized)
        {
            // Reset material when no longer magnetized
            if (meshRenderer != null && originalMaterial != null)
            {
                meshRenderer.material = originalMaterial;
            }
            isMagnetized = false;
            currentSpeed = 0f;
            
            // Normal floating behavior
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            float yOffset = Mathf.Sin((Time.time + timeOffset) * floatSpeed) * floatHeight;
            transform.position = startPosition + new Vector3(0, yOffset, 0);
        }
    }

    private void CollectCoin()
    {
        if (isCollected) return;
        isCollected = true;

        Debug.Log("Coin collected by player!");
        
        // Play collection sound
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position, collectVolume);
        }
        
        // Create floating text
        CreateFloatingText();
        
        // Add coins to the player
        CoinManager.Instance.AddCoins(value);

        // Destroy the coin
        Destroy(gameObject);
    }

    private void CreateFloatingText()
    {
        // Check if the prefab reference is set
        if (floatingTextPrefab != null)
        {
            // Get the main camera for screen position conversion
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                // Convert world position to screen position
                Vector3 screenPos = mainCamera.WorldToScreenPoint(transform.position);
                
                // Create the floating text at the screen position
                GameObject floatingTextObj = Instantiate(floatingTextPrefab, screenPos, Quaternion.identity);
                
                // Make sure it's a child of the Canvas
                Canvas canvas = GameObject.FindObjectOfType<Canvas>();
                if (canvas != null)
                {
                    floatingTextObj.transform.SetParent(canvas.transform, false);
                    
                    // Set the amount using the coin's value
                    CoinFloatingText floatingText = floatingTextObj.GetComponent<CoinFloatingText>();
                    if (floatingText != null)
                    {
                        floatingText.SetAmount(value);
                    }
                }
                else
                {
                    Debug.LogError("No Canvas found in the scene!");
                }
            }
        }
        else
        {
            Debug.LogError("Floating Text Prefab not assigned! Please assign it in the inspector.");
        }
    }

    // Optional: Visualize magnet range in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, magnetRange);
    }
} 