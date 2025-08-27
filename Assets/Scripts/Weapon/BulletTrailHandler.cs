// 8/26/2025 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using UnityEngine;

public class BulletTrailHandler : MonoBehaviour
{
    public GameObject bulletTrail; // Reference to the particle system prefab or child object

    private void OnDestroy()
    {
        if (bulletTrail != null)
        {
            // Detach the particle system from the bullet
            bulletTrail.transform.parent = null;

            // Allow the particle system to finish playing before destroying it
            var particleSystem = bulletTrail.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                Destroy(bulletTrail, particleSystem.main.duration + particleSystem.main.startLifetime.constantMax);
            }
            else
            {
                Destroy(bulletTrail);
            }
        }
    }
}