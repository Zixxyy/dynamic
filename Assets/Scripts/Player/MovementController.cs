using UnityEngine;
using UnityEngine.InputSystem;

public class MovementController : MonoBehaviour
{
    [SerializeField] private CharacterController _characterController;
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private Transform _checkGround;
    [SerializeField] private LayerMask _groundMask;

    [Header("Base Settings")]
    [SerializeField] private float _checkRadiusSphere = 0.3f;
    [SerializeField] private float _gravity = -30f;
    [SerializeField] private float _jumpHeight = 3f;

    // sens same as valorant like 0.5 in val same 0.5 there
    [SerializeField] private float _sensitivity = 0.7f;

    [Header("Movement")]
    [SerializeField] private float _speedWalk = 6f;
    [SerializeField] private float _speedRun = 10f;

    [SerializeField] private float _groundAccelTime = 0.03f;

    [SerializeField] private float _groundDecelTime = 0.02f;
    // in air moves controlling
    [SerializeField] private float _airControlTime = 0.4f;

    [Header("Falling")]
    [SerializeField] private float _terminalVelocity = -40f;
    [SerializeField] private float _slopeLimit = 30f;
    // max slide speed at 90 deg slope
    [SerializeField] private float _slopeSlideMaxSpeed = 12f;

    [Header("Jump Assist")]
    [SerializeField] private float _coyoteTimeDuration = 0.15f;
    [SerializeField] private float _jumpBufferDuration = 0.10f;
    [SerializeField] private float _jumpDelay = 0.02f;
    // early release multiplier higher or shorter jumpink (i forgot that function popular hipsters name)
    [SerializeField] private float _jumpCutMultiplier = 3f;
    
    // ── speed multiplier — driven by PlayerHealthSystem ──

        private float _speedMultiplier = 1f;
 
    public void SetSpeedMultiplier(float multiplier)
    {
        _speedMultiplier = Mathf.Max(0f, multiplier);
    }


    // sensitivity calibration

    private const float SENS_SCALE = 0.07f;

    bool _isGrounded;
    // steep slope state
    bool _isOnSteepSlope;
    float _rotationX;
    float _jumpDelayTimer;  

    Vector3 _horizontalVelocity; 
    Vector3 _verticalVelocity;
    // slide push dir+speed baked each frame 
    Vector3 _slopeSlideVelocity;

    float _coyoteTimer;
    float _jumpBufferTimer;

    InputAction _lookAction;
    InputAction _moveAction;
    InputAction _sprintAction; 
    InputAction _jumpAction;
 
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        _lookAction   = InputSystem.actions.FindAction("Look");
        _moveAction   = InputSystem.actions.FindAction("Move");
        _sprintAction = InputSystem.actions.FindAction("Sprint"); 
        _jumpAction   = InputSystem.actions.FindAction("Jump");

        _characterController.slopeLimit = _slopeLimit;
    }

    void Update()
    {
        Rotate();
        UpdateGroundCheck();
        UpdateTimers();
        Move();
        ApplyGravityAndJump(); 

        _characterController.Move((_horizontalVelocity + _verticalVelocity + _slopeSlideVelocity) * Time.deltaTime);
    }

    // Camera
    private void Rotate()
    {
        Vector2 look = _lookAction.ReadValue<Vector2>() * (_sensitivity * SENS_SCALE);

        _rotationX -= look.y;
        _rotationX = Mathf.Clamp(_rotationX, -65f, 65f);

        _cameraTransform.localRotation = Quaternion.Euler(_rotationX, 0f, 0f);
        transform.Rotate(Vector3.up * look.x);
    }

    // ─── floor ──── 
    private void UpdateGroundCheck()
    {
        _isGrounded = Physics.CheckSphere(_checkGround.position, _checkRadiusSphere, _groundMask);

        // reset slope state before recalc 
        _isOnSteepSlope     = false;
        _slopeSlideVelocity = Vector3.zero;

        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 2f, _groundMask))
        {
            float angle = Vector3.Angle(hit.normal, Vector3.up);

            if (angle > _slopeLimit)
            {
                _isOnSteepSlope = true;

                // project gravity dir onto slope surface = slide direction
                Vector3 slideDir = Vector3.ProjectOnPlane(Vector3.down, hit.normal).normalized;

                // steeper = faster, linear remap [slopeLimit..90] -> [0..maxSpeed] dont try to understand its just a falling when you trying to jump on a angle
                float t = (angle - _slopeLimit) / (90f - _slopeLimit);
                _slopeSlideVelocity = slideDir * (_slopeSlideMaxSpeed * t);
            }
            else if (angle > 0f && _isGrounded)
            {
                // snap to slope so CheckSphere doesn't lose contact
                _verticalVelocity.y = -2f;
            }
        }
    }

    // ─── timers ────
    private void UpdateTimers()
    {
        // Coyote time when on floor timer is filled
        if (_isGrounded)
            _coyoteTimer = _coyoteTimeDuration;
        else
            _coyoteTimer -= Time.deltaTime;

        // Jump buffer
        if (_jumpAction.triggered)
            _jumpBufferTimer = _jumpBufferDuration;
        else
            _jumpBufferTimer -= Time.deltaTime;

        if (_jumpDelayTimer > 0f)
            _jumpDelayTimer -= Time.deltaTime;
    }

    // ─── h moves ───────────────
    private void Move()
    {
        // no steering on steep slope - let physics do its thing
        if (_isOnSteepSlope)
        {
            float t = 1f - Mathf.Exp(-Time.deltaTime / _groundDecelTime);
            _horizontalVelocity = Vector3.Lerp(_horizontalVelocity, Vector3.zero, t);
            return;
        }

        Vector2 input = _moveAction.ReadValue<Vector2>();
        Vector3 wishDir = (transform.forward * input.y + transform.right * input.x);
        if (wishDir.sqrMagnitude > 1f) wishDir.Normalize();

        bool hasInput    = input.sqrMagnitude > 0.01f;
        bool runningBack = input.y < 0f;
        float targetSpeed = 0f;

        if (hasInput)
        {
            // backsteps only walking no running
            targetSpeed = (!runningBack && _sprintAction.IsPressed())
            ? _speedRun  * _speedMultiplier
            : _speedWalk * _speedMultiplier;
        }

        Vector3 targetVelocity = wishDir * targetSpeed;

        if (_isGrounded)
        {
            if (hasInput)
            {
                //frame-rate independent -- exp
                float t = 1f - Mathf.Exp(-Time.deltaTime / _groundAccelTime);
                _horizontalVelocity = Vector3.Lerp(_horizontalVelocity, targetVelocity, t);
            }
            else
            {
                // on floor fast stopping
                float t = 1f - Mathf.Exp(-Time.deltaTime / _groundDecelTime);
                _horizontalVelocity = Vector3.Lerp(_horizontalVelocity, Vector3.zero, t);

                if (_horizontalVelocity.sqrMagnitude < 0.001f)
                    _horizontalVelocity = Vector3.zero;
            }
        }
        else
        {
            // in air
            if (hasInput)
            {
                float t = 1f - Mathf.Exp(-Time.deltaTime / _airControlTime);
                _horizontalVelocity = Vector3.Lerp(_horizontalVelocity, targetVelocity, t);
            }
            // in air inertion
        }
    }

    // ─── v moves and jump ─────
    private void ApplyGravityAndJump()
    {
        // reset v speed on floor
        if (_isGrounded && _verticalVelocity.y < 0f)
            _verticalVelocity.y = -2f;

        // anti eats shit, like dont fly on the floor when jumping
        if ((_characterController.collisionFlags & CollisionFlags.Above) != 0 && _verticalVelocity.y > 0f)
            _verticalVelocity.y = 0f;

        // no jump from steep slope
        if (_jumpBufferTimer > 0f && _coyoteTimer > 0f && _jumpDelayTimer <= 0f && !_isOnSteepSlope)
        {
            _verticalVelocity.y = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
            _jumpDelayTimer  = _jumpDelay;
            _jumpBufferTimer = 0f;
            _coyoteTimer     = 0f;
        }

        // gravitation on'ing
        if (!_isGrounded || _verticalVelocity.y > 0f)
            _verticalVelocity.y += _gravity * Time.deltaTime;

        // early release = cut jump height 
        if (_verticalVelocity.y > 0f && !_jumpAction.IsPressed())
            _verticalVelocity.y += _gravity * (_jumpCutMultiplier - 1f) * Time.deltaTime;

        // speed in header var
        if (_verticalVelocity.y < _terminalVelocity)
            _verticalVelocity.y = _terminalVelocity;
    }
}