using UnityEngine;

public class UIWorldBillboard : MonoBehaviour
{
    public bool matchCameraUp = true;
    public Vector3 worldOffset = new Vector3(0f, 2f, 0f);

    Transform _t;

    void Awake()
    {
        _t = transform;
    }

    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null) return;
        // Keep offset (for when this script is on a child, offset is relative to parent world pos)
        if (_t.parent != null)
        {
            _t.position = _t.parent.position + worldOffset;
        }

        var fwd = cam.transform.forward;
        var up = matchCameraUp ? cam.transform.up : Vector3.up;
        _t.rotation = Quaternion.LookRotation(fwd, up);
    }
}



