using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CameraZone : MonoBehaviour
{
	[Header("Blend Target State")]
	[Tooltip("Offset to use while inside this zone (world space, relative to target)")]
	public Vector3 zoneOffset = new Vector3(0f, 14f, -10f);

	[Tooltip("Zoom to use while inside this zone (1 = default distance). If < 0, keep current.")]
	public float zoneZoom = 1f;

	[Tooltip("Seconds to blend into the zone camera state")] public float blendInSeconds = 1.5f;
	[Tooltip("Seconds to blend back to previous camera state")] public float blendOutSeconds = 1.0f;
	public AnimationCurve blendCurve = AnimationCurve.EaseInOut(0,0,1,1);

	[Header("Behavior")]
	[Tooltip("If true, disables mouse wheel zoom while inside the zone")] public bool lockMouseZoomInside = true;

	Vector3 _prevOffset;
	float _prevZoom;
	bool _hadPrev;
	CameraController _cam;

	void Reset()
	{
		var col = GetComponent<Collider>();
		col.isTrigger = true;
	}

	void OnTriggerEnter(Collider other)
	{
		if (!IsPlayer(other)) return;
		EnsureCamera();
		if (_cam == null) return;
		_prevOffset = _cam.baseOffset;
		_prevZoom = _cam.CurrentZoom;
		_hadPrev = true;
		if (lockMouseZoomInside) _cam.allowMouseZoom = false;
		_cam.BlendTo(zoneOffset, zoneZoom >= 0f ? zoneZoom : (float?)null, Mathf.Max(0.01f, blendInSeconds), blendCurve);
	}

	void OnTriggerExit(Collider other)
	{
		if (!IsPlayer(other)) return;
		EnsureCamera();
		if (_cam == null) return;
		if (lockMouseZoomInside) _cam.allowMouseZoom = true;
		if (_hadPrev)
		{
			_cam.BlendTo(_prevOffset, _prevZoom, Mathf.Max(0.01f, blendOutSeconds), blendCurve);
		}
	}

	bool IsPlayer(Collider c)
	{
		return c.CompareTag("Player") || c.GetComponentInParent<Player>() != null;
	}

	void EnsureCamera()
	{
		if (_cam != null) return;
		var cam = Camera.main;
		if (cam != null) _cam = cam.GetComponent<CameraController>();
	}
}






