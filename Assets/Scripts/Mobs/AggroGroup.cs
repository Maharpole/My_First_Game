using UnityEngine;
using System.Collections.Generic;

public class AggroGroup : MonoBehaviour
{
    public float aggroRadius = 10f;
    public float leashRadius = 20f;

    private List<GameObject> members = new List<GameObject>();
    private Transform player;

    public void Register(GameObject enemy)
    {
        if (!members.Contains(enemy)) members.Add(enemy);
        // Ensure members start idle
        var ctrl = enemy.GetComponent<EnemyController>();
        if (ctrl != null)
        {
            ctrl.ClearAggro();
        }
    }

    void Start()
    {
        var p = FindObjectOfType<Player>();
        player = p != null ? p.transform : null;
    }

    void Update()
    {
        if (player == null) return;

        // Check proximity against any member
        foreach (var m in members)
        {
            if (m == null) continue;
            if ((m.transform.position - player.position).sqrMagnitude <= aggroRadius * aggroRadius)
            {
                AlertAll();
                break;
            }
        }

        // Optional: simple leash (not fully implemented here)
        // Could track a pack center and clear aggro if player far beyond leashRadius
    }

    public void OnMemberDamaged()
    {
        AlertAll();
    }

    void AlertAll()
    {
        foreach (var m in members)
        {
            if (m == null) continue;
            var controller = m.GetComponent<EnemyController>();
            if (controller != null)
            {
                controller.ForceAggro(player);
            }
        }
    }
}
