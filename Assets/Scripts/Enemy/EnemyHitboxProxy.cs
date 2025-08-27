using UnityEngine;

/// Attach this to a large trigger collider (child of the enemy).
/// It forwards bullet hits to the owning EnemyHealth on the parent.
public class EnemyHitboxProxy : MonoBehaviour
{
    public EnemyHealth owner;

    void Reset()
    {
        if (owner == null) owner = GetComponentInParent<EnemyHealth>();
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    public EnemyHealth Resolve()
    {
        if (owner == null) owner = GetComponentInParent<EnemyHealth>();
        return owner;
    }
}


