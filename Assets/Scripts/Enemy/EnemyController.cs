using UnityEngine;
using System.Collections;

public class EnemyController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("How fast the enemy moves towards the player")]
    public float moveSpeed = 5f;

    [Header("Aggro Settings")]
    [Tooltip("If true, the enemy starts aggroed")] public bool startAggro = false;

    private Transform playerTransform;
    private Rigidbody rb;
    private bool isInitialized = false;
    private bool isAggro = false;

    void Start()
    {
        try
        {
            // Find the player object
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                Debug.LogError("Player not found! Make sure your player has the 'Player' tag.");
                return;
            }

            // Set up physics components
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }

            // Configure Rigidbody
            rb.freezeRotation = true;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            // Ensure there's a collider
            if (GetComponent<Collider>() == null)
            {
                CapsuleCollider capsule = gameObject.AddComponent<CapsuleCollider>();
                capsule.height = 2.0f;
                capsule.radius = 0.5f;
                capsule.center = new Vector3(0, 1.0f, 0);
            }

            isInitialized = true;
            isAggro = startAggro;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error initializing EnemyController: {e.Message}");
            isInitialized = false;
        }
    }

    void Update()
    {
        if (!isInitialized) return;

        try
        {
            if (!isAggro)
            {
                // idle: do nothing; ensure we aren't drifting
                if (rb != null) rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
                return;
            }

            if (playerTransform != null)
            {
                // Calculate direction to player (ignoring Y axis)
                Vector3 toPlayer = (playerTransform.position - transform.position);
                Vector3 direction = toPlayer;
                direction.y = 0;
                direction = direction.normalized;

                // Move towards player
                if (rb != null)
                {
                    rb.velocity = new Vector3(direction.x * moveSpeed, rb.velocity.y, direction.z * moveSpeed);
                }

                // Face the player
                if (direction != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(direction);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in EnemyController Update: {e.Message}");
        }
    }

    public void ForceAggro(Transform target)
    {
        playerTransform = target;
        isAggro = true;
    }

    public void ClearAggro()
    {
        isAggro = false;
        if (rb != null) rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
    }

    void OnDestroy()
    {
        // Clean up any resources if needed
        playerTransform = null;
        rb = null;
    }
}