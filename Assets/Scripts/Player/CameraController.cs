using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Target the camera follows. If empty, will try Player.Instance or an object tagged 'Player'.")]
    public Transform target;

    [Header("Follow Settings")]
    [Tooltip("Base offset from the target in world space")] 
    public Vector3 baseOffset = new Vector3(0f, 12f, -7f);

    [Tooltip("Smoothly interpolate camera movement")] 
    public bool smoothFollow = true;

    [Tooltip("How quickly the camera follows the target when smoothing is enabled")] 
    [Range(1f, 20f)] public float followLerpSpeed = 8f;

    [Header("Zoom Settings")]
    [Tooltip("Use orthographic zoom (size) instead of moving the camera closer/farther")] 
    public bool useOrthographicZoom = true;

    [Tooltip("Mouse wheel zoom sensitivity")] 
    [Range(0.05f, 2f)] public float zoomSensitivity = 0.2f;

    [Tooltip("Minimum zoom (smaller = closer for ortho; shorter distance for perspective)")] 
    public float minZoom = 0.5f;

    [Tooltip("Maximum zoom (larger = farther for ortho; longer distance for perspective)")] 
    public float maxZoom = 2.5f;

    [Tooltip("Starting zoom value")] 
    public float initialZoom = 1f;

    private Camera unityCamera;
    private float currentZoom;
    private Vector3 currentVelocity;

    void Awake()
    {
        unityCamera = GetComponent<Camera>();
        if (unityCamera == null)
        {
            Debug.LogError("CameraController requires a Camera component on the same GameObject.");
        }

        // Resolve target if not assigned
        if (target == null)
        {
            var playerSingleton = FindObjectOfType<Player>();
            if (playerSingleton != null)
            {
                target = playerSingleton.transform;
            }
            else
            {
                var playerByTag = GameObject.FindGameObjectWithTag("Player");
                if (playerByTag != null) target = playerByTag.transform;
            }
        }

        currentZoom = Mathf.Clamp(initialZoom, minZoom, maxZoom);

        // Place camera immediately at the desired starting position
        if (target != null)
        {
            Vector3 desired = ComputeDesiredPosition();
            transform.position = desired;
            transform.LookAt(target.position);
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        HandleZoomInput();

        Vector3 desiredPos = ComputeDesiredPosition();
        if (smoothFollow)
        {
            // Exponential-like smoothing
            transform.position = Vector3.Lerp(transform.position, desiredPos, 1f - Mathf.Exp(-followLerpSpeed * Time.deltaTime));
        }
        else
        {
            transform.position = desiredPos;
        }

        transform.LookAt(target.position);
    }

    Vector3 ComputeDesiredPosition()
    {
        if (useOrthographicZoom && unityCamera != null && unityCamera.orthographic)
        {
            // For ortho, keep offset fixed and change size
            return target.position + baseOffset;
        }
        else
        {
            // For perspective (or ortho without ortho-zoom), scale distance along the offset direction
            Vector3 dir = baseOffset.sqrMagnitude > 0.0001f ? baseOffset.normalized : Vector3.back;
            float baseDistance = baseOffset.magnitude;
            float distance = baseDistance * currentZoom;
            return target.position + dir * distance;
        }
    }

    void HandleZoomInput()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) < 0.0001f) return;

        currentZoom = Mathf.Clamp(currentZoom - scroll * zoomSensitivity, minZoom, maxZoom);

        if (useOrthographicZoom && unityCamera != null)
        {
            unityCamera.orthographic = true; // ensure ortho for ortho zoom
            // In orthographic, "zoom" increases size for farther view
            unityCamera.orthographicSize = Mathf.Max(0.01f, currentZoom * 8f); // 8 is a friendly baseline; tweak as needed
        }
    }
}
