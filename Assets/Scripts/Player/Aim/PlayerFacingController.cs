using UnityEngine;

/// <summary>
/// Rotates the player root to face the AimProvider's flat aim direction (yaw-only), with turn speed.
/// Industry-standard separation: Input -> AimProvider; Controller -> root yaw; Rig -> upper body; Weapon -> independent.
/// </summary>
public class PlayerFacingController : MonoBehaviour
{
	[Header("References")] public AimProvider aim;
	[Tooltip("Which transform to rotate (usually the player root)")] public Transform rotateRoot;

	[Header("Tuning")] [Tooltip("If aim magnitude is below this, facing won't update")] public float minAimDirSqrMag = 0.0001f;

	void Awake()
	{
		if (rotateRoot == null) rotateRoot = transform;
		if (aim == null) aim = GetComponent<AimProvider>() ?? GetComponentInParent<AimProvider>();
	}

	void Update()
	{
		if (aim == null || rotateRoot == null) return;
		Vector3 dir = aim.AimDirectionFlat; // already flattened and normalized when valid
		if (dir.sqrMagnitude < minAimDirSqrMag) return;
		Quaternion target = Quaternion.LookRotation(dir, Vector3.up);
		rotateRoot.rotation = target; // instant snap
	}
}


