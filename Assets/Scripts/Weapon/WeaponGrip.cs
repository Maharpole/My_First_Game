using UnityEngine;

// Author on each weapon prefab to mark standardized attachment points.
// RightHandGrip: where the right hand holds the weapon (primary attach).
// LeftHandGrip: where the left hand should be placed via IK (optional).
// Muzzle: bullet spawn/aim forward reference.
public class WeaponGrip : MonoBehaviour
{
	[Header("Grip Points")]
	public Transform rightHandGrip;
	public Transform leftHandGrip;

	[Header("Muzzle / Aim")]
	public Transform muzzle;
	[Tooltip("If true, fall back to searching common names if fields are not set.")]
	public bool autoFindIfMissing = true;

	[Header("Fine Tune (Optional)")]
	[Tooltip("Local rotation offset (degrees) applied after aligning rightHandGrip to the hand.")]
	public Vector3 rightHandRotationOffsetEuler;
	[Tooltip("Local position offset applied after aligning rightHandGrip to the hand (in weapon local space).")]
	public Vector3 rightHandPositionOffset;

	[Header("Adjustable Offset Transform (Optional)")]
	[Tooltip("If assigned, this child transform's local TRS is applied after alignment. Use this to tweak per-weapon position/rotation by editing the child in prefab mode.")]
	public Transform alignmentOffset;

	[Header("Rotation Locks (Optional)")]
	[Tooltip("Force the equipped weapon's local X rotation to 0 after alignment.")]
	public bool lockLocalXRotationToZero = false;

	[Header("Ground Stabilization (Optional)")]
	[Tooltip("Keep weapon parallel to ground (removes pitch/roll), useful if character leans.")]
	public bool stabilizeParallelToGround = false;
	[Tooltip("Maintain a target height above ground (world-space). Set < 0 to disable.")]
	public float stabilizeTargetHeight = -1f;
	[Tooltip("How quickly to interpolate to target height (units/sec). 0 = snap.")]
	public float stabilizeHeightLerpSpeed = 20f;
	[Tooltip("How quickly to level the weapon (deg/sec). 0 = snap.")]
	public float stabilizeRotationLerpSpeed = 360f;
	public LayerMask stabilizeGroundMask = ~0;

	void OnValidate()
	{
		if (!autoFindIfMissing) return;
		if (rightHandGrip == null) rightHandGrip = FindByNames(transform, "RightHandGrip", "R_Grip", "Grip_R", "Handle");
		if (leftHandGrip == null) leftHandGrip = FindByNames(transform, "LeftHandGrip", "L_Grip", "Grip_L", "Support");
		if (muzzle == null) muzzle = FindByNames(transform, "Muzzle", "MuzzlePoint", "BarrelEnd", "Tip");
	}

	Transform FindByNames(Transform root, params string[] names)
	{
		for (int i = 0; i < names.Length; i++)
		{
			var t = FindRecursive(root, names[i]);
			if (t != null) return t;
		}
		return null;
	}

	Transform FindRecursive(Transform root, string name)
	{
		if (root == null || string.IsNullOrEmpty(name)) return null;
		if (root.name == name) return root;
		for (int i = 0; i < root.childCount; i++)
		{
			var c = root.GetChild(i);
			var r = FindRecursive(c, name);
			if (r != null) return r;
		}
		return null;
	}
}
