using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Central, read-only provider for mouse aim in world space.
/// Computes a ground-projected aim point and exposes a flattened aim direction.
/// Other systems (facing, indicators, weapons) read from here to avoid double-drivers.
/// </summary>
public class AimProvider : MonoBehaviour
{
	[Header("Grounding & Masks")] public LayerMask groundMask = ~0;
	[Tooltip("Additional layers to ignore when raycasting (e.g., player layers)")] public LayerMask ignoreMask = 0;
	[Tooltip("Optional override for the Y plane height when fallback plane is used")] public Transform planeHeightFrom;

	[Header("Debug")] public bool drawGizmos = false;
	public Color gizmoAimColor = new Color(1f, 0.3f, 0.2f, 1f);
	public Color gizmoDirColor = Color.yellow;

	Camera _camera;
	Transform _root;

	public Vector3 AimPoint { get; private set; }
	public Vector3 AimDirectionFlat { get; private set; } // Y removed, normalized

	void Awake()
	{
		_root = transform;
		_camera = Camera.main;
	}

	void Update()
	{
		if (_camera == null) { _camera = Camera.main; if (_camera == null) return; }
		Vector3 point;
		if (TryGetMouseGroundPoint(out point))
		{
			AimPoint = point;
			Vector3 from = _root != null ? _root.position : Vector3.zero;
			Vector3 dir = AimPoint - from; dir.y = 0f;
			AimDirectionFlat = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.forward;
		}
	}

	bool TryGetMouseGroundPoint(out Vector3 worldPoint)
	{
		worldPoint = Vector3.zero;
		if (_camera == null) return false;
		Vector2 mp;
#if ENABLE_INPUT_SYSTEM
		mp = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
		mp = Input.mousePosition;
#endif
		Ray ray = _camera.ScreenPointToRay(mp);
		int mask = groundMask & ~ignoreMask;
		var hits = Physics.RaycastAll(ray, 1000f, mask, QueryTriggerInteraction.Ignore);
		if (hits != null && hits.Length > 0)
		{
			System.Array.Sort(hits, (a,b) => a.distance.CompareTo(b.distance));
			for (int i = 0; i < hits.Length; i++)
			{
				var h = hits[i];
				if (h.collider == null) continue;
				if (_root != null && h.collider.transform.IsChildOf(_root)) continue;
				worldPoint = h.point; return true;
			}
		}
		float planeY = planeHeightFrom != null ? planeHeightFrom.position.y : (_root != null ? _root.position.y : 0f);
		Plane plane = new Plane(Vector3.up, new Vector3(0f, planeY, 0f));
		if (plane.Raycast(ray, out float t)) { worldPoint = ray.GetPoint(t); return true; }
		return false;
	}

	void OnDrawGizmosSelected()
	{
		if (!drawGizmos) return;
		Gizmos.color = gizmoAimColor;
		Gizmos.DrawSphere(AimPoint, 0.06f);
		if (_root != null)
		{
			Gizmos.color = gizmoDirColor;
			Gizmos.DrawRay(_root.position, AimDirectionFlat * 0.8f);
		}
	}
}


