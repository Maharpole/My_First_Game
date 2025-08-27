// 8/26/2025 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using UnityEngine;

public class BulletTrailHandler : MonoBehaviour
{
    public GameObject bulletTrail; // Reference to the particle system prefab or child object

    public void DetachTrailNow()
    {
        if (bulletTrail == null) return;

        bulletTrail.transform.SetParent(null, true);

        var particleSystem = bulletTrail.GetComponent<ParticleSystem>();
        if (particleSystem != null)
        {
            var main = particleSystem.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.None;
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            Destroy(bulletTrail, main.duration + main.startLifetime.constantMax);
        }
        else
        {
            Destroy(bulletTrail, 2f);
        }

        bulletTrail = null;
    }

    private void OnDestroy()
    {
        if (bulletTrail != null)
        {
            // Fallback detach in case caller forgot to detach explicitly
            DetachTrailNow();
        }
    }
}