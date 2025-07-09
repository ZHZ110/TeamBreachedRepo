using UnityEngine;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM 
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour
    {
        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 2.0f;

        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 5.335f;

        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.3f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Header("Swimming Audio")]
        public AudioSource swimmingAudioSource;
        public AudioClip swimmingSound;
        [Range(0, 1)] public float SwimmingAudioVolume = 0.5f;
        [Range(0.8f, 2.0f)] public float NormalSwimPitch = 1.0f;
        [Range(0.8f, 2.0f)] public float SprintSwimPitch = 1.5f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;

        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 70.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;

        [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;

        [Header("Floating Movement")]
        [Tooltip("Enable floating movement (disables gravity)")]
        public bool FloatingMode = false;

        [Tooltip("Distance to move up/down with each key press")]
        public float VerticalStepDistance = 1.0f;

        [Tooltip("Speed of the smooth up/down movement")]
        public float VerticalMoveSpeed = 5.0f;

        [Tooltip("Minimum Y position (prevents going below floor)")]
        public float MinimumYPosition = 0.0f;

        // Private variables for smooth vertical movement
        private Vector3 _targetVerticalPosition;
        private bool _isMovingVertically = false;
        private bool _isSwimming = false;

        // Reference to PlayerRockPusher to check if pushing
        private PlayerRockPusher _rockPusher;

        // cinemachine
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // player
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

#if ENABLE_INPUT_SYSTEM 
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;

        private const float _threshold = 0.01f;
        private bool _hasAnimator;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
                return false;
#endif
            }
        }

        private void Awake()
        {
            // get a reference to our main camera
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
        }

        private void Start()
        {
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();

            // Get reference to rock pusher script
            _rockPusher = GetComponent<PlayerRockPusher>();

#if ENABLE_INPUT_SYSTEM
            _playerInput = GetComponent<PlayerInput>();
#else
            //Debug.LogError("Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

            AssignAnimationIDs();

            // reset our timeouts on start
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;

            // Initialize target position for floating movement
            _targetVerticalPosition = transform.position;
        }

        private void Update()
        {
            _hasAnimator = TryGetComponent(out _animator);

            if (FloatingMode)
            {
                // In floating mode, only handle movement and camera, skip gravity and grounded checks
                Move();
            }
            else
            {
                // Normal mode - run all systems
                JumpAndGravity();
                GroundedCheck();
                Move();
            }
        }

        private void LateUpdate()
        {
            CameraRotation();
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        private void GroundedCheck()
        {
            // set sphere position, with offset
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
                QueryTriggerInteraction.Ignore);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }

        private void CameraRotation()
        {
            // Lock camera to always look forward in the same direction as the player
            _cinemachineTargetYaw = transform.eulerAngles.y;  // Follow player's Y rotation
            _cinemachineTargetPitch = 0.0f;  // Keep looking straight ahead (no up/down tilt)

            // Apply the rotation to the camera target
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw, 0.0f);
        }

        private void Move()
        {
            // set target speed based on move speed, sprint speed and if sprint is pressed
            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

            // if there is no input, set the target speed to 0
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            // a reference to the players current horizontal velocity
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            // accelerate or decelerate to target speed
            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // creates curved result rather than a linear one giving a more organic speed change
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                    Time.deltaTime * SpeedChangeRate);

                // round speed to 3 decimal places
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            // Check if player is pushing a rock
            bool isPushingRock = _rockPusher != null && _rockPusher.IsPushing();

            // Check if whale should be swimming (but not if pushing rocks)
            bool shouldBeSwimming = _input.move != Vector2.zero && _speed > 0.1f && !isPushingRock;
            bool isSprinting = _input.sprint && shouldBeSwimming;

            // Handle swimming audio
            if (shouldBeSwimming && !_isSwimming)
            {
                // Start swimming sound
                if (swimmingAudioSource && swimmingSound)
                {
                    swimmingAudioSource.clip = swimmingSound;
                    swimmingAudioSource.volume = SwimmingAudioVolume;
                    swimmingAudioSource.loop = true;
                    swimmingAudioSource.pitch = isSprinting ? SprintSwimPitch : NormalSwimPitch;
                    swimmingAudioSource.Play();
                    //Debug.Log("Swimming audio started");
                }
                _isSwimming = true;
            }
            else if (shouldBeSwimming && _isSwimming)
            {
                // Update pitch while swimming (for sprint transitions)
                if (swimmingAudioSource)
                {
                    swimmingAudioSource.pitch = isSprinting ? SprintSwimPitch : NormalSwimPitch;
                }
            }
            else if ((!shouldBeSwimming || isPushingRock) && _isSwimming)
            {
                // Stop swimming sound (either not moving or pushing rocks)
                if (swimmingAudioSource)
                {
                    swimmingAudioSource.Stop();
                    //Debug.Log($"Swimming audio stopped. Reason: {(isPushingRock ? "Pushing rock" : "Not moving")}");
                }
                _isSwimming = false;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            // normalise input direction
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            // Always update target rotation when there's input (even if not moving)
            if (_input.move != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                  _mainCamera.transform.eulerAngles.y;
            }

            // Always smoothly rotate towards target rotation (whether moving or stationary)
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                RotationSmoothTime);

            // rotate to face input direction relative to camera position
            transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);

            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            if (FloatingMode)
            {
                // Handle vertical movement to get the vertical velocity
                float verticalMovement = HandleFloatingVerticalMovement();

                // Create combined movement vector
                Vector3 horizontalMovement = targetDirection.normalized * (_speed * Time.deltaTime);
                Vector3 combinedMovement = new Vector3(horizontalMovement.x, verticalMovement, horizontalMovement.z);

                // Apply combined movement in a single call
                CollisionFlags collisionFlags = _controller.Move(combinedMovement);

                // Check for ceiling collision and stop vertical movement if needed
                if (collisionFlags == CollisionFlags.Above && verticalMovement > 0)
                {
                    _isMovingVertically = false;
                    //Debug.Log("Hit ceiling, stopping upward movement");
                }

                _verticalVelocity = 0f;
            }
            else
            {
                // Normal mode - use CharacterController with gravity
                if (_verticalVelocity < _terminalVelocity)
                {
                    _verticalVelocity += Gravity * Time.deltaTime;
                }

                _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                                 new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
            }

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }

        private float HandleFloatingVerticalMovement()
        {
            // Use Input System Keyboard directly for reliable key detection
            bool floatUpPressed = false;
            bool floatDownPressed = false;

            if (Keyboard.current != null)
            {
                floatUpPressed = Keyboard.current.eKey.wasPressedThisFrame;
                floatDownPressed = Keyboard.current.qKey.wasPressedThisFrame;
            }

            // Set new target position when keys are pressed
            if (floatUpPressed && !_isMovingVertically)
            {
                _targetVerticalPosition = new Vector3(transform.position.x, transform.position.y + VerticalStepDistance, transform.position.z);
                _isMovingVertically = true;
            }
            else if (floatDownPressed && !_isMovingVertically)
            {
                // Check if moving down would go below minimum Y position
                float newY = transform.position.y - VerticalStepDistance;
                if (newY >= MinimumYPosition)
                {
                    _targetVerticalPosition = new Vector3(transform.position.x, newY, transform.position.z);
                }
                else
                {
                    // Target the minimum Y position instead
                    _targetVerticalPosition = new Vector3(transform.position.x, MinimumYPosition, transform.position.z);
                }
                _isMovingVertically = true;
            }

            // Calculate vertical movement delta
            float verticalMovementDelta = 0f;

            if (_isMovingVertically)
            {
                float distanceToTarget = Mathf.Abs(_targetVerticalPosition.y - transform.position.y);

                if (distanceToTarget > 0.05f) // Still moving towards target
                {
                    float direction = Mathf.Sign(_targetVerticalPosition.y - transform.position.y);
                    verticalMovementDelta = direction * VerticalMoveSpeed * Time.deltaTime;

                    // Prevent overshooting the target
                    if (Mathf.Abs(verticalMovementDelta) > distanceToTarget)
                    {
                        verticalMovementDelta = direction * distanceToTarget;
                        _isMovingVertically = false; // We've reached the target
                    }
                }
                else // Close enough to target, snap and stop
                {
                    verticalMovementDelta = _targetVerticalPosition.y - transform.position.y;
                    _isMovingVertically = false;
                }
            }

            return verticalMovementDelta;
        }

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                _fallTimeoutDelta = FallTimeout;

                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }
                }

                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                _jumpTimeoutDelta = JumpTimeout;

                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }

                _input.jump = false;
            }

            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius);
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    var index = Random.Range(0, FootstepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
                }
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }
    }
}