using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerHandIK : MonoBehaviour
{
	[Header("Targets")]
	public Transform leftHandTarget;
	public Transform rightHandTarget; // optional

	[Header("Weights")] public float leftPositionWeight = 1f;
	public float leftRotationWeight = 1f;
	public float rightPositionWeight = 0f;
	public float rightRotationWeight = 0f;

	Animator _anim;

	void Awake()
	{
		_anim = GetComponent<Animator>();
	}

	void OnAnimatorIK(int layerIndex)
	{
		if (_anim == null) return;
		if (leftHandTarget != null)
		{
			_anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, Mathf.Clamp01(leftPositionWeight));
			_anim.SetIKRotationWeight(AvatarIKGoal.LeftHand, Mathf.Clamp01(leftRotationWeight));
			_anim.SetIKPosition(AvatarIKGoal.LeftHand, leftHandTarget.position);
			_anim.SetIKRotation(AvatarIKGoal.LeftHand, leftHandTarget.rotation);
		}
		else
		{
			_anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
			_anim.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
		}

		if (rightHandTarget != null)
		{
			_anim.SetIKPositionWeight(AvatarIKGoal.RightHand, Mathf.Clamp01(rightPositionWeight));
			_anim.SetIKRotationWeight(AvatarIKGoal.RightHand, Mathf.Clamp01(rightRotationWeight));
			_anim.SetIKPosition(AvatarIKGoal.RightHand, rightHandTarget.position);
			_anim.SetIKRotation(AvatarIKGoal.RightHand, rightHandTarget.rotation);
		}
		else
		{
			_anim.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
			_anim.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
		}
	}

	public void SetLeftHandTarget(Transform t)
	{
		leftHandTarget = t;
	}
}
