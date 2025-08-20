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
    [Tooltip("If true, also writes the Player.moveSpeed (configured speed) into this Animator float parameter.")]
    public bool usePlayerMoveSpeed = false;
    public string playerSpeedParam = "MoveSpeed";

#if ENABLE_INPUT_SYSTEM
    [Header("Input (New Input System)")]
    [Tooltip("Action that represents the player's attack/primary fire (performed sets attack bool).")]
    public InputActionReference attackAction;
#endif

    [Header("Speed Smoothing")] public float speedLerp = 12f;

    private Animator _anim;
    private Player _player;
    private Vector3 _lastPos;
    private float _smoothedSpeed;
    private bool _hasSpeedParam, _hasAttackParam, _hasPlayerSpeedParam, _hasRangedTrigger, _hasMeleeTrigger;

    void Awake()
    {
        _anim = GetComponent<Animator>();
        _lastPos = transform.position;
        _player = Player.Instance ?? FindFirstObjectByType<Player>();
        CacheAnimatorParams();
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
        // Compute horizontal speed from world displacement to be agnostic of movement system
        Vector3 pos = transform.position;
        float rawSpeed = (pos - _lastPos).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        _lastPos = pos;

        // Ignore vertical component
        _smoothedSpeed = Mathf.Lerp(_smoothedSpeed, rawSpeed, 1f - Mathf.Exp(-speedLerp * Time.deltaTime));

        if (_hasSpeedParam)
        {
            _anim.SetFloat(speedParam, _smoothedSpeed);
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
        }
    }

    // Called by weapon systems
    public void FlagRangedFired()
    {
        if (_hasRangedTrigger) _anim.SetTrigger(rangedFireTrigger);
    }

    // Expose melee hook for future melee controller
    public void FlagMeleeFired()
    {
        if (_hasMeleeTrigger) _anim.SetTrigger(meleeFireTrigger);
    }
}


