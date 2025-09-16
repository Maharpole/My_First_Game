using UnityEngine;

[RequireComponent(typeof(CharacterEquipment))]
public class WeaponEquipper : MonoBehaviour
{
	[Header("Attach Sockets")] public Transform rightHand;
	public Transform leftHand;
	[Tooltip("Fallback if a hand is not assigned")] public Transform attachPoint;

	public enum AttachMode { HandGrip, Anchor }
	[Header("Attach Mode")] public AttachMode attachMode = AttachMode.Anchor;
	[Tooltip("Anchor in front of the player for simple top-down aiming.")] public Transform weaponAnchor;
	[Tooltip("Local offset of the weapon from the anchor.")] public Vector3 anchorLocalOffset = new Vector3(0f, 1f, 0.5f);
	[Tooltip("Follow player yaw (Y rotation) only.")] public bool anchorFollowYaw = true;
	[Tooltip("Min distance from ground when using anchor.")] public float anchorMinGroundHeight = 1.0f;
	[Tooltip("If false, disables any anchor ground clamping so height never adjusts.")] public bool anchorUseGroundClamp = false;
	public LayerMask anchorGroundMask = ~0;

	[Header("Options")] public bool useGripMarkers = true;
	public bool useLeftHandIK = true;
	public string fallbackAnchorChildName = "Controller";
	public string fallbackMuzzleChildName = "Muzzle";

	CharacterEquipment _equipment;
	GameObject _currentMain;
	GameObject _currentOff;

	// Stabilization state derived from WeaponGrip on equipped weapon
	Transform _stabilizeRoot;
	bool _stabilizeParallelToGround = false;
	float _stabilizeTargetHeight = -1f;
	float _stabilizeHeightLerpSpeed = 20f;
	float _stabilizeRotationLerpSpeed = 360f;
	LayerMask _stabilizeGroundMask = ~0;

	[Header("Stabilization Overrides")] 
	[Tooltip("Force-disable weapon height stabilization regardless of WeaponGrip settings.")] public bool forceDisableWeaponHeightStabilize = true;
	[Tooltip("Force-enable keeping weapon parallel to ground (removes pitch/roll)." )] public bool forceParallelToGround = true;

	void Awake()
	{
		_equipment = GetComponent<CharacterEquipment>();
		if (attachPoint == null) attachPoint = transform;
	}

	void OnEnable()
	{
		if (_equipment != null) _equipment.onEquipmentChanged.AddListener(Refresh);
	}

	void OnDisable()
	{
		if (_equipment != null) _equipment.onEquipmentChanged.RemoveListener(Refresh);
	}

	void Start()
	{
		// Ensure we attempt to spawn on scene start
		Refresh();
	}

	[ContextMenu("Refresh Weapon Attachments")]
	public void Refresh()
	{
		Clear();
		if (attachMode == AttachMode.Anchor) EnsureAnchor();
		if (_equipment == null)
		{
			Debug.LogWarning("[WeaponEquipper] Missing CharacterEquipment component; cannot refresh.");
			return;
		}
		var slot = _equipment.mainHand;
		if (slot == null)
		{
			Debug.Log("[WeaponEquipper] mainHand slot is null (not initialized yet?)");
			return;
		}
		if (!slot.HasItem)
		{
			Debug.Log("[WeaponEquipper] No item in mainHand; nothing to attach.");
			return;
		}
		if (slot.EquippedItem == null)
		{
			Debug.Log("[WeaponEquipper] mainHand.HasItem but EquippedItem is null; skipping.");
			return;
		}
		if (!slot.EquippedItem.isWeapon)
		{
			Debug.Log($"[WeaponEquipper] Item '{slot.EquippedItem.equipmentName}' is not marked as weapon; skipping spawn.");
			return;
		}

		var prefab = slot.EquippedItem.modelPrefab;
		Transform parent = attachMode == AttachMode.Anchor ? ResolveAnchorParent() : Resolve(rightHand, attachPoint);
		if (parent == transform)
		{
			Debug.LogWarning("[WeaponEquipper] rightHand and attachPoint were not assigned/valid; falling back to player root.");
		}
		if (prefab == null)
		{
			Debug.LogWarning($"[WeaponEquipper] Equipped weapon '{slot.EquippedItem.equipmentName}' has no modelPrefab; nothing to spawn.");
			return;
		}
		_currentMain = Instantiate(prefab, parent);
		_currentMain.name = "EquippedWeapon";
		var root = _currentMain.transform;
		root.localPosition = Vector3.zero;
		root.localRotation = Quaternion.identity;
		root.localScale = Vector3.one;
		Debug.Log($"[WeaponEquipper] Spawned '{slot.EquippedItem.equipmentName}' under '{parent.name}'.");
		if (attachMode == AttachMode.HandGrip) AlignWeapon(root);
		// Ensure muzzle and fire profile are wired before arming
		WireMuzzle(_currentMain);
		ApplyFireProfile(_currentMain);
		WireLeftHandIK(_currentMain);
		// Arm shooter after successful equip
		var shooter = GetComponentInParent<ClickShooter>() ?? GetComponent<ClickShooter>();
		if (shooter != null)
		{
			shooter.isArmed = true;
			// Ensure shooter does not rotate the player unless explicitly desired
			if (attachMode == AttachMode.Anchor && weaponAnchor != null)
			{
				shooter.faceTarget = weaponAnchor;
			}
		}
		SetupStabilization(_currentMain);
	}

	void Clear()
	{
		if (_currentMain != null) DestroySafe(_currentMain);
		_currentMain = null;
		if (_currentOff != null) DestroySafe(_currentOff);
		_currentOff = null;
		// Reset stabilization
		_stabilizeRoot = null;
		_stabilizeParallelToGround = false;
		_stabilizeTargetHeight = -1f;
		_stabilizeHeightLerpSpeed = 20f;
		_stabilizeRotationLerpSpeed = 360f;
		// Disarm shooter when no weapon is equipped
		var shooter = GetComponentInParent<ClickShooter>() ?? GetComponent<ClickShooter>();
		if (shooter != null)
		{
			shooter.isArmed = false;
			shooter.muzzle = null;
		}
	}

	void DestroySafe(GameObject go)
	{
		if (go == null) return;
		if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
	}

	Transform Resolve(Transform a, Transform b) => (a != null && a.gameObject.scene.IsValid()) ? a : (b != null && b.gameObject.scene.IsValid() ? b : transform);

	void AlignWeapon(Transform root)
	{
		if (root == null) return;

		// Normalize local transform after parenting for predictable alignment
		root.localPosition = Vector3.zero;
		root.localRotation = Quaternion.identity;
		root.localScale = Vector3.one;

		if (useGripMarkers)
		{
			var grip = root.GetComponentInChildren<WeaponGrip>();
			if (grip != null && grip.rightHandGrip != null)
			{
				AlignRootToChildAnchor(root, grip.rightHandGrip);
				// Apply optional fine tune offsets (rotation then position)
				if (grip.rightHandRotationOffsetEuler != Vector3.zero)
				{
					root.localRotation = root.localRotation * Quaternion.Euler(grip.rightHandRotationOffsetEuler);
				}
				if (grip.rightHandPositionOffset != Vector3.zero)
				{
					root.localPosition += root.TransformVector(grip.rightHandPositionOffset);
				}
				// Apply alignment offset child, if present
				if (grip.alignmentOffset != null)
				{
					root.localPosition += grip.alignmentOffset.localPosition;
					root.localRotation = root.localRotation * grip.alignmentOffset.localRotation;
				}
				// Optional: lock X rotation to 0 in local space
				if (grip.lockLocalXRotationToZero)
				{
					Vector3 e = root.localEulerAngles;
					e.x = 0f;
					root.localEulerAngles = e;
				}
				return;
			}
		}
		// Fallback: zero + named anchor
		if (!string.IsNullOrEmpty(fallbackAnchorChildName))
		{
			var a = FindChildByName(root, fallbackAnchorChildName);
			if (a != null && a != root) AlignRootToChildAnchor(root, a);
		}
	}

	Transform ResolveAnchorParent()
	{
		if (weaponAnchor != null && weaponAnchor.gameObject.scene.IsValid()) return weaponAnchor;
		// Ensure we have a dedicated child anchor, never rotate the player root
		EnsureAnchor();
		return weaponAnchor != null ? weaponAnchor : transform;
	}

	void EnsureAnchor()
	{
		if (weaponAnchor != null && weaponAnchor.gameObject.scene.IsValid()) return;
		Transform parent = (attachPoint != null && attachPoint.gameObject.scene.IsValid()) ? attachPoint : transform;
		var go = new GameObject("WeaponAnchorRuntime");
		go.transform.SetParent(parent, false);
		go.transform.localPosition = Vector3.zero;
		go.transform.localRotation = Quaternion.identity;
		go.transform.localScale = Vector3.one;
		weaponAnchor = go.transform;
	}
	
	void AlignRootToChildAnchor(Transform root, Transform childAnchor)
	{
		if (root == null || childAnchor == null) return;
		// Robust world-space alignment so that childAnchor lands exactly on root.parent (hand) origin/rotation
		// Works even if childAnchor is deeply nested and avoids issues with intermediate transforms/scales.
		var parent = root.parent;
		if (parent == null) return;
		Quaternion deltaRot = parent.rotation * Quaternion.Inverse(childAnchor.rotation);
		Vector3 deltaPos = parent.position - (deltaRot * childAnchor.position);
		root.rotation = deltaRot * root.rotation;
		root.position = deltaRot * root.position + deltaPos;
	}

	void WireMuzzle(GameObject weapon)
	{
		if (weapon == null) return;
		var shooter = GetComponentInParent<ClickShooter>() ?? GetComponent<ClickShooter>();
		if (shooter == null) return;
		Transform muzzle = null;
		var grip = weapon.GetComponentInChildren<WeaponGrip>();
		if (grip != null && grip.muzzle != null) muzzle = grip.muzzle;
		if (muzzle == null)
		{
			muzzle = FindChildByName(weapon.transform, fallbackMuzzleChildName)
				?? FindChildByName(weapon.transform, "MuzzlePoint")
				?? FindChildByName(weapon.transform, "BarrelEnd")
				?? FindChildByName(weapon.transform, "Tip");
		}
		shooter.muzzle = muzzle != null ? muzzle : weapon.transform;
	}

	void WireLeftHandIK(GameObject weapon)
	{
		if (!useLeftHandIK || weapon == null) return;
		var ik = GetComponentInChildren<PlayerHandIK>();
		if (ik == null) return;
		var grip = weapon.GetComponentInChildren<WeaponGrip>();
		ik.SetLeftHandTarget(grip != null ? grip.leftHandGrip : null);
		// Optional: if you want to drive right hand via IK too, expose rightHandTarget
		// and assign it here. By default we keep right hand driven by the rig.
	}

	void ApplyFireProfile(GameObject weapon)
	{
		if (weapon == null) return;
		var shooter = GetComponentInParent<ClickShooter>() ?? GetComponent<ClickShooter>();
		if (shooter == null) return;
		var profile = weapon.GetComponentInChildren<WeaponFireProfile>();
		if (profile != null) profile.ApplyTo(shooter);
	}

	Transform FindChildByName(Transform root, string name)
	{
		if (root == null || string.IsNullOrEmpty(name)) return null;
		if (root.name == name) return root;
		for (int i = 0; i < root.childCount; i++)
		{
			var c = root.GetChild(i);
			var r = FindChildByName(c, name);
			if (r != null) return r;
		}
		return null;
	}

	void SetupStabilization(GameObject weapon)
	{
		_stabilizeRoot = null;
		_stabilizeParallelToGround = false;
		_stabilizeTargetHeight = -1f;
		if (weapon == null) return;
		var grip = weapon.GetComponentInChildren<WeaponGrip>();
		if (grip == null) return;
		_stabilizeRoot = weapon.transform;
		_stabilizeParallelToGround = forceParallelToGround || grip.stabilizeParallelToGround;
		_stabilizeTargetHeight = forceDisableWeaponHeightStabilize ? -1f : grip.stabilizeTargetHeight;
		_stabilizeHeightLerpSpeed = Mathf.Max(0f, grip.stabilizeHeightLerpSpeed);
		_stabilizeRotationLerpSpeed = Mathf.Max(0f, grip.stabilizeRotationLerpSpeed);
		_stabilizeGroundMask = grip.stabilizeGroundMask;
	}

	void LateUpdate()
	{
		// Drive anchor each frame if in Anchor mode
		if (attachMode == AttachMode.Anchor)
		{
			UpdateAnchorTransform();
		}

		if (_stabilizeRoot == null) return;

		if (_stabilizeParallelToGround)
		{
			Vector3 flat = Vector3.ProjectOnPlane(_stabilizeRoot.forward, Vector3.up);
			if (flat.sqrMagnitude > 0.0001f)
			{
				Quaternion target = Quaternion.LookRotation(flat.normalized, Vector3.up);
				if (_stabilizeRotationLerpSpeed <= 0f) _stabilizeRoot.rotation = target;
				else _stabilizeRoot.rotation = Quaternion.RotateTowards(_stabilizeRoot.rotation, target, _stabilizeRotationLerpSpeed * Time.deltaTime);
			}
		}

		if (_stabilizeTargetHeight >= 0f)
		{
			Vector3 pos = _stabilizeRoot.position;
			float groundY = pos.y - _stabilizeTargetHeight;
			Vector3 origin = new Vector3(pos.x, pos.y + 50f, pos.z);
			if (TryRaycastGroundNoRigidbodies(origin, 200f, _stabilizeGroundMask, out var hit))
			{
				groundY = hit.point.y;
			}
			float desiredY = groundY + _stabilizeTargetHeight;
			if (Mathf.Abs(desiredY - pos.y) > 0.0001f)
			{
				if (_stabilizeHeightLerpSpeed <= 0f) pos.y = desiredY;
				else pos.y = Mathf.Lerp(pos.y, desiredY, Time.deltaTime * _stabilizeHeightLerpSpeed);
				_stabilizeRoot.position = pos;
			}
		}
	}

	void UpdateAnchorTransform()
	{
		if (weaponAnchor == null) return;
		// Never rotate the player root
		if (weaponAnchor == transform) return;
		// Position: from player position plus local offset in anchor's space
		if (anchorFollowYaw)
		{
			Vector3 forward = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
			if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
			weaponAnchor.rotation = Quaternion.LookRotation(forward, Vector3.up);
		}
		weaponAnchor.localPosition = anchorLocalOffset;
		// Ground clamp (optional)
		if (anchorUseGroundClamp && anchorMinGroundHeight > 0f)
		{
			Vector3 pos = weaponAnchor.position;
			float groundY = pos.y - anchorMinGroundHeight;
			Vector3 origin = new Vector3(pos.x, pos.y + 50f, pos.z);
			if (TryRaycastGroundNoRigidbodies(origin, 200f, anchorGroundMask, out var hit))
			{
				groundY = hit.point.y;
			}
			float desiredY = Mathf.Max(pos.y, groundY + anchorMinGroundHeight);
			if (desiredY > pos.y)
			{
				pos.y = desiredY;
				weaponAnchor.position = pos;
			}
		}
	}

	// Ground ray helper that ignores any colliders that have an attached Rigidbody (e.g., trees/rocks)
	static bool TryRaycastGroundNoRigidbodies(Vector3 origin, float maxDistance, LayerMask mask, out RaycastHit best)
	{
		best = default;
		var hits = Physics.RaycastAll(origin, Vector3.down, maxDistance, mask, QueryTriggerInteraction.Ignore);
		if (hits == null || hits.Length == 0) return false;
		float minDist = float.MaxValue;
		for (int i = 0; i < hits.Length; i++)
		{
			var h = hits[i];
			if (h.collider == null) continue;
			// Ignore anything with a Rigidbody to avoid dynamic obstacles affecting weapon height
			if (h.rigidbody != null || (h.collider != null && h.collider.attachedRigidbody != null)) continue;
			if (h.distance < minDist)
			{
				minDist = h.distance;
				best = h;
			}
		}
		return minDist < float.MaxValue;
	}
}


