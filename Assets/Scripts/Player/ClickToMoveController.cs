using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NavMeshAgent))]
public class ClickToMoveController : MonoBehaviour
{
    public LayerMask groundMask;
    public float stopDistance = 1.2f;
    public float itemPickupDistance = 1.6f;
    public float navmeshSampleMaxDistance = 5f;

    private NavMeshAgent agent;
    private Player player;
    private ItemPickup pendingPickup;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        player = GetComponent<Player>();
        if (agent != null)
        {
            agent.stoppingDistance = stopDistance;
            // We'll handle rotation manually to face movement direction
            agent.updateRotation = false;
        }
    }

    void Start()
    {
        // Try to ensure we are on a NavMesh (if baked). This prevents errors if the player starts slightly off.
        if (agent != null && !agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out var hit, navmeshSampleMaxDistance, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
        }
    }

    void Update()
    {
        // Interrupt by WASD input
        var kb = Keyboard.current;
        if (kb != null)
        {
            float h = (kb.dKey.isPressed ? 1f : 0f) + (kb.aKey.isPressed ? -1f : 0f);
            float v = (kb.wKey.isPressed ? 1f : 0f) + (kb.sKey.isPressed ? -1f : 0f);
            if (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f)
            {
                CancelClickMove();
            }
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandleClick();
        }

        // If we are moving to pick an item, check range and pick up when close
        if (pendingPickup != null && agent != null && agent.isOnNavMesh && !agent.pathPending)
        {
            if (!agent.hasPath || agent.remainingDistance <= itemPickupDistance)
            {
                pendingPickup.TryPickup(player, itemPickupDistance);
                pendingPickup = null;
            }
        }

        // Face agent velocity when using click-to-move
        if (agent != null && agent.isOnNavMesh && player != null && player.isActiveAndEnabled)
        {
            Vector3 vel = agent.velocity;
            vel.y = 0f;
            if (vel.sqrMagnitude > 0.001f)
            {
                Quaternion target = Quaternion.LookRotation(vel.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, target, player.rotationSpeed * Time.deltaTime);
            }
        }
    }

    void HandleClick()
    {
        if (Camera.main == null) return;
        Vector2 mousePos = Mouse.current?.position.ReadValue() ?? Vector2.zero;
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        if (Physics.Raycast(ray, out var hit, 500f))
        {
            // Item click?
            var pickup = hit.collider.GetComponentInParent<ItemPickup>();
            if (pickup != null)
            {
                MoveTo(hit.point);
                pendingPickup = pickup;
                return;
            }

            // Otherwise, move to ground point
            if (((1 << hit.collider.gameObject.layer) & groundMask) != 0)
            {
                MoveTo(hit.point);
            }
        }
    }

    void MoveTo(Vector3 worldPos)
    {
        if (agent == null || !agent.isOnNavMesh) return;
        if (NavMesh.SamplePosition(worldPos, out var hit, navmeshSampleMaxDistance, NavMesh.AllAreas))
        {
            agent.isStopped = false;
            agent.SetDestination(hit.position);
        }
    }

    public void CancelClickMove()
    {
        if (agent == null || !agent.isOnNavMesh) return;
        agent.isStopped = true;
        agent.ResetPath();
        pendingPickup = null;
    }
}
