using UnityEngine;

public class PlayerSpawnPoint : MonoBehaviour
{
    [Tooltip("Identifier for this spawn point. Portals can target this by id.")]
    public string spawnId;

    [Tooltip("Optional facing direction override for the player on spawn.")]
    public Vector3 faceDirection = Vector3.forward;

    public Vector3 Position => transform.position;
    public Quaternion Rotation => faceDirection == Vector3.zero ? transform.rotation : Quaternion.LookRotation(new Vector3(faceDirection.x, 0f, faceDirection.z).normalized, Vector3.up);
}


