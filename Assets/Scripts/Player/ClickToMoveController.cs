using UnityEngine;
using UnityEngine.AI;

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
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        if (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f)
        {
            CancelClickMove();
        }

        if (Input.GetMouseButtonDown(0))
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
    }

    void HandleClick()
    {
        if (Camera.main == null) return;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
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
