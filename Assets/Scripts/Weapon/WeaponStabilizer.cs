using UnityEngine;

public class WeaponStabilizer : MonoBehaviour
{
	[Header("Parallel-Leveling")]
	public bool levelToGround = false;
	public float rotationLerpSpeed = 360f;

	[Header("Height Stabilization")]
	public bool maintainHeight = false;
	public float targetHeight = 1.2f;
	public float heightLerpSpeed = 20f;
	public LayerMask groundMask = ~0;

	Transform _root;

	void Awake()
	{
		_root = transform;
	}

	void LateUpdate()
	{
		if (_root == null) return;

		if (levelToGround)
		{
			Quaternion target = Quaternion.LookRotation(Vector3.ProjectOnPlane(_root.forward, Vector3.up).normalized, Vector3.up);
			if (rotationLerpSpeed <= 0f) _root.rotation = target;
			else _root.rotation = Quaternion.RotateTowards(_root.rotation, target, rotationLerpSpeed * Time.deltaTime);
		}

		if (maintainHeight && targetHeight >= 0f)
		{
			float currentY = _root.position.y;
			float groundY = SampleGroundY(_root.position);
			float desiredY = groundY + targetHeight;
			if (Mathf.Abs(desiredY - currentY) > 0.0001f)
			{
				float t = heightLerpSpeed <= 0f ? 1f : Mathf.Clamp01(Time.deltaTime * heightLerpSpeed);
				_root.position = new Vector3(_root.position.x, Mathf.Lerp(currentY, desiredY, t), _root.position.z);
			}
		}
	}

	float SampleGroundY(Vector3 at)
	{
		// Cast down from above to find ground
		Vector3 origin = new Vector3(at.x, at.y + 50f, at.z);
		if (Physics.Raycast(origin, Vector3.down, out var hit, 200f, groundMask, QueryTriggerInteraction.Ignore))
		{
			return hit.point.y;
		}
		// Fallback: keep current height baseline
		return at.y - targetHeight;
	}
}



