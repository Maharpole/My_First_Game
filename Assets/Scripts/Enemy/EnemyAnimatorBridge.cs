using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Animator))]
public class EnemyAnimatorBridge : MonoBehaviour
{
	public string speedParameter = "Speed";
	public string isMovingParameter = "IsMoving";
	public string isAttackingParameter = "IsAttacking";
	public string hitTrigger = "Hit";
	public string isDeadBool = "IsDead";
	[Tooltip("Seconds to smooth speed changes")]
	public float smoothingSeconds = 0.1f;
	[Tooltip("Speed below this is considered idle")]
	public float movingThreshold = 0.05f;

	private Animator animator;
	private NavMeshAgent navAgent;
	private Rigidbody rb;
	private Transform motionRoot;
	private Vector3 lastPosition;
	private float smoothedSpeed;

	private int speedHash;
	private int isMovingHash;
	private int isAttackingHash;
	private int hitHash;
	private int isDeadHash;
	private bool hasSpeedParam;
	private bool hasIsMovingParam;
	private bool hasIsAttackingParam;
	private bool hasHitTrigger;
	private bool hasIsDeadBool;

	void Awake()
	{
		animator = GetComponent<Animator>();
		// Animator may live on a child, while movement is on the root
		navAgent = GetComponent<NavMeshAgent>();
		if (navAgent == null) navAgent = GetComponentInParent<NavMeshAgent>();
		rb = GetComponent<Rigidbody>();
		if (rb == null) rb = GetComponentInParent<Rigidbody>();
		motionRoot = (navAgent != null) ? navAgent.transform : (rb != null ? rb.transform : transform);
		lastPosition = motionRoot.position;

		speedHash = Animator.StringToHash(speedParameter);
		isMovingHash = Animator.StringToHash(isMovingParameter);
		isAttackingHash = Animator.StringToHash(isAttackingParameter);
		hitHash = Animator.StringToHash(hitTrigger);
		isDeadHash = Animator.StringToHash(isDeadBool);
		hasSpeedParam = AnimatorHasParameter(animator, speedHash);
		hasIsMovingParam = AnimatorHasParameter(animator, isMovingHash);
		hasIsAttackingParam = AnimatorHasParameter(animator, isAttackingHash);
		hasHitTrigger = AnimatorHasParameter(animator, hitHash);
		hasIsDeadBool = AnimatorHasParameter(animator, isDeadHash);
	}

	void Update()
	{
		float rawSpeed = 0f;

		if (navAgent != null && navAgent.enabled)
		{
			float desired = navAgent.desiredVelocity.magnitude;
			float actual = navAgent.velocity.magnitude;
			rawSpeed = Mathf.Max(desired, actual);
		}
		else if (rb != null)
		{
			// Unity 2021+ safe; if using newer APIs, keep velocity for compatibility
			rawSpeed = rb.linearVelocity.magnitude;
		}
		else
		{
			Vector3 pos = motionRoot.position;
			rawSpeed = (pos - lastPosition).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
			lastPosition = pos;
		}

		// Exponential smoothing
		float k = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.0001f, smoothingSeconds));
		smoothedSpeed = Mathf.Lerp(smoothedSpeed, rawSpeed, k);

		bool moving = smoothedSpeed > movingThreshold;

		if (hasSpeedParam) animator.SetFloat(speedHash, smoothedSpeed);
		if (hasIsMovingParam) animator.SetBool(isMovingHash, moving);
	}

	public void SetIsAttacking(bool value)
	{
		if (hasIsAttackingParam) animator.SetBool(isAttackingHash, value);
	}

	public void FlagHit()
	{
		if (hasHitTrigger) animator.SetTrigger(hitTrigger);
	}

	public void SetIsDead(bool value)
	{
		if (hasIsDeadBool) animator.SetBool(isDeadBool, value);
	}

	// Animation Event proxies (place events on the Animator object)
	public void AE_AttackStart()
	{
		var eh = GetComponentInParent<EnemyHealth>();
		if (eh != null) eh.Animation_AttackStart();
	}

	public void AE_MeleeStrike()
	{
		var eh = GetComponentInParent<EnemyHealth>();
		if (eh != null) eh.Animation_MeleeStrike();
	}

	public void AE_AttackEnd()
	{
		var eh = GetComponentInParent<EnemyHealth>();
		if (eh != null) eh.Animation_AttackEnd();
	}

	private static bool AnimatorHasParameter(Animator a, int nameHash)
	{
		for (int i = 0; i < a.parameterCount; i++)
		{
			if (a.GetParameter(i).nameHash == nameHash) return true;
		}
		return false;
	}
}