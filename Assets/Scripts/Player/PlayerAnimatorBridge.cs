using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Drives Animator parameters from actual player movement and (optional) attack input,
/// without taking over movement. Safe to add alongside existing Player movement systems.
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerAnimatorBridge : MonoBehaviour
{
    [Header("Animator")] public string speedParam = "Speed";
    public string attackBoolParam = "IsAttacking";
    public string rangedFireTrigger = "RangedFire";
    public string meleeFireTrigger = "MeleeFire";
    [Tooltip("Animator trigger name to play dash animation.")]
    public string dashTrigger = "Dash";
    [Tooltip("Animator bool name to indicate the player is currently dashing.")]
    public string isDashingBool = "IsDashing";
    [Tooltip("If true, also writes the Player.moveSpeed (configured speed) into this Animator float parameter.")]
    public bool usePlayerMoveSpeed = false;
    public string playerSpeedParam = "MoveSpeed";

    [Header("Facing & Movement Output")]
    [Tooltip("Degrees per second to rotate the player to the facing direction.")]
    public float rotationSpeed = 540f;
    [Tooltip("If true, this component will rotate the player each frame. If false, rotation is left to other systems.")]
    public bool driveFacing = false;
    [Tooltip("If driving facing, prefer aiming direction during the aim window.")]
    public bool preferAimFacing = true;
    [Tooltip("If driving facing, avoid changing facing while the dash bool is true.")]
    public bool suppressFacingWhileDashing = true;
    [Tooltip("Optional: output directional locomotion for a 2D blend tree (local space). Leave empty to disable.")]
    public string moveXParam = "MoveX";
    public string moveZParam = "MoveZ";
    public bool writeDirectionalMove = false;

    [Header("Upper Body Layer (Shooting)")]
    [Tooltip("Animator layer name that contains upper body shooting poses. Leave empty to use layer index instead.")]
    public string upperBodyLayerName = "";
    [Tooltip("Animator layer index to control if name is empty.")]
    public int upperBodyLayerIndex = 1;
    [Tooltip("How quickly the upper body layer weight blends.")]
    public float upperBodyWeightLerp = 12f;

    [Header("Aim Hold")]
    [Tooltip("How long after firing we keep facing the aim direction (seconds)")]
    public float aimHoldSeconds = 0.25f;

#if ENABLE_INPUT_SYSTEM
    [Header("Input (New Input System)")]
    [Tooltip("Action that represents the player's attack/primary fire (performed sets attack bool).")]
    public InputActionReference attackAction;
#endif

    [Header("Speed Smoothing")] public float speedLerp = 12f;

    private Animator _anim;
    private Player _player;
    private Rigidbody _rb;
    private Vector3 _lastPos;
    private float _smoothedSpeed;
    private bool _hasSpeedParam, _hasAttackParam, _hasPlayerSpeedParam, _hasRangedTrigger, _hasMeleeTrigger;
    private bool _hasMoveX, _hasMoveZ;
    private bool _hasDashTrigger, _hasIsDashingBool;
    private Transform _facingRoot;
    private Vector3 _lastReportedAim;
    private float _lastFiredTime;
    [System.NonSerialized] public bool IsAiming;

    void Awake()
    {
        _anim = GetComponent<Animator>();
        _lastPos = transform.position;
        _player = Player.Instance ?? FindFirstObjectByType<Player>();
        _rb = (_player != null) ? _player.GetComponent<Rigidbody>() : GetComponentInParent<Rigidbody>();
        _facingRoot = (_player != null) ? _player.transform : transform.root;
        CacheAnimatorParams();
        // Debug: report parameter availability
        var ctrl = _anim != null ? _anim.runtimeAnimatorController : null;
        Debug.Log($"[AnimBridge] Animator='{(ctrl!=null?ctrl.name:"<none>")}' DashParam='{dashTrigger}' found={_hasDashTrigger} IsDashing='{isDashingBool}' found={_hasIsDashingBool}");
    }

    void OnEnable()
    {
#if ENABLE_INPUT_SYSTEM
        if (attackAction != null)
        {
            attackAction.action.performed += OnAttackPerformed;
            attackAction.action.canceled += OnAttackCanceled;
            attackAction.action.Enable();
        }
#endif
    }

    void OnDisable()
    {
#if ENABLE_INPUT_SYSTEM
        if (attackAction != null)
        {
            attackAction.action.performed -= OnAttackPerformed;
            attackAction.action.canceled -= OnAttackCanceled;
            attackAction.action.Disable();
        }
#endif
    }

    void Update()
    {
        // Compute movement from Rigidbody if available, else via displacement
        Vector3 worldDisplacement;
        float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
        Vector3 rawMove = Vector3.zero;
        if (_rb != null)
        {
            rawMove = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
            worldDisplacement = rawMove * deltaTime;
        }
        else
        {
            Vector3 pos = _facingRoot != null ? _facingRoot.position : transform.position;
            worldDisplacement = pos - _lastPos;
            worldDisplacement.y = 0f;
            rawMove = worldDisplacement / deltaTime;
            _lastPos = pos;
        }

        float rawSpeed = worldDisplacement.magnitude / deltaTime;

        // Ignore vertical component
        _smoothedSpeed = Mathf.Lerp(_smoothedSpeed, rawSpeed, 1f - Mathf.Exp(-speedLerp * Time.deltaTime));

        if (_hasSpeedParam)
        {
            _anim.SetFloat(speedParam, _smoothedSpeed);
        }

        // Optional directional locomotion output for blend trees
        if (writeDirectionalMove && (_hasMoveX || _hasMoveZ))
        {
            Transform basis = _facingRoot != null ? _facingRoot : transform;
            Vector3 local = basis.InverseTransformDirection(rawMove);
            if (_hasMoveX) _anim.SetFloat(moveXParam, local.x);
            if (_hasMoveZ) _anim.SetFloat(moveZParam, local.z);
        }

        // Determine if we are currently considered firing
        bool isFiringNow = (Time.time - _lastFiredTime) <= aimHoldSeconds;
        IsAiming = isFiringNow && _lastReportedAim.sqrMagnitude > 0.001f;

        // Drive facing only if enabled
        if (driveFacing)
        {
            bool isDashingNow = false;
            if (suppressFacingWhileDashing && _hasIsDashingBool && _anim != null)
            {
                isDashingNow = _anim.GetBool(isDashingBool);
            }

            Vector3 desiredForward = (_facingRoot != null ? _facingRoot.forward : transform.forward);
            if (!isDashingNow)
            {
                if (preferAimFacing && isFiringNow && _lastReportedAim.sqrMagnitude > 0.001f)
                {
                    desiredForward = _lastReportedAim;
                }
                else if (rawMove.sqrMagnitude > 0.001f)
                {
                    desiredForward = rawMove.normalized;
                }
            }

            if (desiredForward.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(desiredForward, Vector3.up);
                Transform rotRoot = _facingRoot != null ? _facingRoot : transform;
                rotRoot.rotation = Quaternion.RotateTowards(rotRoot.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }
        }

        // Blend upper body layer weight while firing
        int layerIndex = ResolveUpperBodyLayerIndex();
        if (layerIndex >= 0 && layerIndex < _anim.layerCount)
        {
            float current = _anim.GetLayerWeight(layerIndex);
            float target = isFiringNow ? 1f : 0f;
            float blended = Mathf.Lerp(current, target, 1f - Mathf.Exp(-upperBodyWeightLerp * Time.deltaTime));
            _anim.SetLayerWeight(layerIndex, blended);
        }

        if (usePlayerMoveSpeed && _hasPlayerSpeedParam)
        {
            if (_player == null) _player = Player.Instance ?? FindFirstObjectByType<Player>();
            if (_player != null)
            {
                _anim.SetFloat(playerSpeedParam, _player.moveSpeed);
            }
        }
    }

#if ENABLE_INPUT_SYSTEM
    void OnAttackPerformed(InputAction.CallbackContext _)
    {
        if (_hasAttackParam) _anim.SetBool(attackBoolParam, true);
    }

    void OnAttackCanceled(InputAction.CallbackContext _)
    {
        if (_hasAttackParam) _anim.SetBool(attackBoolParam, false);
    }
#endif

    void CacheAnimatorParams()
    {
        _hasSpeedParam = false;
        _hasAttackParam = false;
        _hasPlayerSpeedParam = false;
        _hasRangedTrigger = false;
        _hasMeleeTrigger = false;
        _hasMoveX = false;
        _hasMoveZ = false;
        if (_anim == null) return;
        var ps = _anim.parameters;
        for (int i = 0; i < ps.Length; i++)
        {
            var p = ps[i];
            if (!string.IsNullOrEmpty(speedParam) && p.name == speedParam && p.type == AnimatorControllerParameterType.Float) _hasSpeedParam = true;
            if (!string.IsNullOrEmpty(attackBoolParam) && p.name == attackBoolParam && p.type == AnimatorControllerParameterType.Bool) _hasAttackParam = true;
            if (usePlayerMoveSpeed && !string.IsNullOrEmpty(playerSpeedParam) && p.name == playerSpeedParam && p.type == AnimatorControllerParameterType.Float) _hasPlayerSpeedParam = true;
            if (!string.IsNullOrEmpty(rangedFireTrigger) && p.name == rangedFireTrigger && p.type == AnimatorControllerParameterType.Trigger) _hasRangedTrigger = true;
            if (!string.IsNullOrEmpty(meleeFireTrigger) && p.name == meleeFireTrigger && p.type == AnimatorControllerParameterType.Trigger) _hasMeleeTrigger = true;
            if (writeDirectionalMove && !string.IsNullOrEmpty(moveXParam) && p.name == moveXParam && p.type == AnimatorControllerParameterType.Float) _hasMoveX = true;
            if (writeDirectionalMove && !string.IsNullOrEmpty(moveZParam) && p.name == moveZParam && p.type == AnimatorControllerParameterType.Float) _hasMoveZ = true;
            if (!string.IsNullOrEmpty(dashTrigger) && p.name == dashTrigger && p.type == AnimatorControllerParameterType.Trigger) _hasDashTrigger = true;
            if (!string.IsNullOrEmpty(isDashingBool) && p.name == isDashingBool && p.type == AnimatorControllerParameterType.Bool) _hasIsDashingBool = true;
        }
    }

    // Called by weapon systems
    public void FlagRangedFired()
    {
        _lastFiredTime = Time.time;
        if (_hasRangedTrigger) _anim.SetTrigger(rangedFireTrigger);
    }

    // Expose melee hook for future melee controller
    public void FlagMeleeFired()
    {
        _lastFiredTime = Time.time;
        if (_hasMeleeTrigger) _anim.SetTrigger(meleeFireTrigger);
    }

    // Called by Player to signal dash start
    public void FlagDashStarted()
    {
        Debug.Log($"[AnimBridge] FlagDashStarted() hasDashTrigger={_hasDashTrigger} hasIsDashingBool={_hasIsDashingBool}");
        if (_hasDashTrigger) _anim.SetTrigger(dashTrigger);
        if (_hasIsDashingBool) _anim.SetBool(isDashingBool, true);
        // Fail-safe: ensure we actually enter the dash state even if current state isn't Run
        if (_anim != null)
        {
            try { _anim.CrossFade("Dashing", 0f, 0); } catch { }
        }
    }

    // Called by Player to signal dash end
    public void FlagDashEnded()
    {
        Debug.Log($"[AnimBridge] FlagDashEnded() hasIsDashingBool={_hasIsDashingBool}");
        if (_hasIsDashingBool) _anim.SetBool(isDashingBool, false);
    }

    // Called by weapon systems to indicate where shots are aimed
    public void ReportAimDirection(Vector3 worldDirection)
    {
        worldDirection.y = 0f;
        if (worldDirection.sqrMagnitude > 0.0001f)
        {
            _lastReportedAim = worldDirection.normalized;
        }
    }

    private int ResolveUpperBodyLayerIndex()
    {
        if (_anim == null) return -1;
        if (!string.IsNullOrEmpty(upperBodyLayerName))
        {
            for (int i = 0; i < _anim.layerCount; i++)
            {
                if (_anim.GetLayerName(i) == upperBodyLayerName) return i;
            }
        }
        return upperBodyLayerIndex;
    }
}


