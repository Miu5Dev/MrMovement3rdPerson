using System;
using UnityEngine;

[RequireComponent(typeof(PhysicsController))]
public class PlayerController : MonoBehaviour
{
  PhysicsController physics;
  
  [Header("Camera Settings")]
  public Transform CameraTransform;
  
  [Header("Gravity Settings")]
  public float Gravity = -9.81f;
  [Space(20)]
  [Header("Grounded Settings")]
  public float MovingSpeed = 12f;
  public float MinStartSpeed = 3f;
  public float Acceleration = 10f;
  public float Deceleration = 15f;
  
  [Space(20)]
  [Header("Airbounded Settings")]
  public float AirMovingSpeed = 4f;
  public float AirAcceleration = 2f;      
  public float AirDeceleration = 1.5f;     
  public float AirDragDeceleration = 0.8f;
  
  [Space(20)]
  [Header("Rotation Settings")]
  public float RotationSpeed = 15f;
  public float AirRotationSpeed = 8f;
  
  [Space(20)]
  [Header("Jump Settings")]
  public float JumpForce = 10f;
  public float MinJumpForce = 4f;
  public float CoyoteTime = 0.12f;        
  public float JumpBufferTime = 0.1f;     

  [Space(20)]
  [Header("Curve Turn Settings")]
  public float CurveTurnSpeed = 5f;
  public float ReversalAngle = 150f;

  [Space(20)]
  [Header("Air Rotation Lock Settings")]
  public float AirReversalLockAngle = 120f;
  
  private Vector3 Velocity;
  private bool isGrounded;
  private bool isOnSteepSlope;
  private bool isAction;
  private bool isJump;
  private bool isCrouch;
  private bool isSwap;
  private Vector2 Direction;
  private float coyoteTimer;
  private float jumpBufferTimer;
  private bool hasJumped;
  private Vector2 smoothedDirection;
  private bool airRotationLocked;
  private Quaternion lockedRotation;
  private Vector3 takeoffDirection;
  private bool isDryJump;
  private Vector2 cameraRelativeDirection;
  private bool jumpHeld;
  private bool jumpCut;
  
  [Space(40)]
  [Header("------------ DEBUG ---------------------------------------------------------")] 
  public bool GroundedDebug = false;
  public bool ActionDebug = false;
  public bool JumpDebug = false;
  public bool CrouchDebug = false;
  public bool SwapDebug = false;
  [Space(10)] 
  public float speedDebug;
  [Space(10)]
  public Vector2 DirectionDebug;
  [Space(10)]
  public Vector3 VelocityDebug;
  [Space(10)]
  public bool DryJumpDebug = false;
  
  void Awake() 
  {
    physics = GetComponent<PhysicsController>();
  }

  void Start()
  {
    if (CameraTransform == null && Camera.main != null)
      CameraTransform = Camera.main.transform;

    EventBus.Subscribe<OnActionInputEvent>(@event => isAction = @event.pressed);
    EventBus.Subscribe<OnCrouchInputEvent>(@event => isCrouch = @event.pressed);
    EventBus.Subscribe<OnJumpInputEvent>(OnJumpInput);
    EventBus.Subscribe<OnSwapInputEvent>(@event => isSwap = @event.pressed);
    EventBus.Subscribe<OnMoveInputEvent>(@event => Direction = @event.Direction);
  }

  private void OnJumpInput(OnJumpInputEvent evt)
  {
    if (evt.pressed)
    {
      isJump = true;
      jumpHeld = true;
    }
    else
    {
      isJump = false;
      jumpHeld = false;
      if (!isGrounded && Velocity.y > 0f && hasJumped)
        jumpCut = true;
    }
  }

  void FixedUpdate()
  {
    HandleCameraRelativeInput();
    HandleGrounded();
    HandleGravity();
    HandleJump();
    HandleVariableJump();
    HandleMovement();
    HandleRotation();
    HandleMotion();
    HandleDebug();
  }

  // ========================================================================
  // Forward = dirección cámara → jugador (horizontal, normalizada)
  // Esto es lo que permite el orbiting: como la cámara tiene lag,
  // este vector cambia cada frame cuando el jugador se mueve lateral.
  // ========================================================================
  private void HandleCameraRelativeInput()
  {
    if (Direction.sqrMagnitude < 0.01f || CameraTransform == null)
    {
      cameraRelativeDirection = Vector2.zero;
      return;
    }

    Vector3 forward = transform.position - CameraTransform.position;
    forward.y = 0f;

    if (forward.sqrMagnitude < 0.01f)
    {
      cameraRelativeDirection = Vector2.zero;
      return;
    }

    forward.Normalize();
    Vector3 right = new Vector3(forward.z, 0f, -forward.x);

    Vector3 world = forward * Direction.y + right * Direction.x;
    cameraRelativeDirection = new Vector2(world.x, world.z);

    if (cameraRelativeDirection.sqrMagnitude > 1f)
      cameraRelativeDirection.Normalize();
  }

  private void HandleGravity()
  {
    if (isGrounded)
      Velocity.y = -2f;
    else
      Velocity.y += Gravity * Time.fixedDeltaTime;
  }

  private void HandleJump()
  {
    if (isGrounded)
    {
      coyoteTimer = CoyoteTime;
      hasJumped = false;
      isDryJump = false;
      jumpCut = false;
    }
    else
    {
      coyoteTimer -= Time.fixedDeltaTime;
    }

    if (isJump)
      jumpBufferTimer = JumpBufferTime;
    else
      jumpBufferTimer -= Time.fixedDeltaTime;

    bool canJump = coyoteTimer > 0f && !hasJumped;

    if (jumpBufferTimer > 0f && canJump)
    {
      bool hasDirection = cameraRelativeDirection.sqrMagnitude > 0.01f;
      Vector2 horizontalVel = new Vector2(Velocity.x, Velocity.z);
      bool hasSpeed = horizontalVel.sqrMagnitude >= MinStartSpeed * MinStartSpeed;

      if (hasDirection && !hasSpeed)
      {
        Vector3 inputDir = new Vector3(cameraRelativeDirection.x, 0f, cameraRelativeDirection.y).normalized;
        Velocity.x = inputDir.x * MinStartSpeed;
        Velocity.z = inputDir.z * MinStartSpeed;
      }

      isDryJump = !hasDirection && !hasSpeed;
      takeoffDirection = new Vector3(Velocity.x, 0f, Velocity.z).normalized;

      Velocity.y = JumpForce;
      hasJumped = true;
      jumpCut = false;
      coyoteTimer = 0f;
      jumpBufferTimer = 0f;
    }
  }

  private void HandleVariableJump()
  {
    if (jumpCut && Velocity.y > 0f)
    {
      Velocity.y = Mathf.Min(Velocity.y, MinJumpForce);
      jumpCut = false;
    }
  }
  
  private void HandleRotation()
  {
    Vector3 horizontalVelocity = new Vector3(Velocity.x, 0f, Velocity.z);

    if (isGrounded)
    {
      airRotationLocked = false;
      if (horizontalVelocity.sqrMagnitude < 0.01f) return;
      Quaternion targetRotation = Quaternion.LookRotation(horizontalVelocity.normalized);
      transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, RotationSpeed * Time.fixedDeltaTime);
      return;
    }

    if (isDryJump) return;
    if (horizontalVelocity.sqrMagnitude < 0.01f) return;

    if (!airRotationLocked && takeoffDirection.sqrMagnitude > 0.01f)
    {
      Vector3 inputDir3D = new Vector3(cameraRelativeDirection.x, 0f, cameraRelativeDirection.y);
      if (inputDir3D.sqrMagnitude > 0.01f)
      {
        float angleToTakeoff = Vector3.Angle(takeoffDirection, inputDir3D.normalized);
        if (angleToTakeoff >= AirReversalLockAngle)
        {
          airRotationLocked = true;
          lockedRotation = transform.rotation;
        }
      }
    }

    if (airRotationLocked)
    {
      transform.rotation = lockedRotation;
      return;
    }

    Vector3 velocityDir = horizontalVelocity.normalized;
    Quaternion airTargetRotation = Quaternion.LookRotation(velocityDir);
    transform.rotation = Quaternion.Slerp(transform.rotation, airTargetRotation, AirRotationSpeed * Time.fixedDeltaTime);
  }
  
  private void HandleMotion()
  {
    MoveResult result = physics.Move(Velocity * Time.fixedDeltaTime);
    
    if (result.collided)
    {
      foreach (var collision in result.collisions)
      {
        if (collision.angle > 80f && collision.angle < 135f)
        {
          Vector3 normal = collision.normal;
          float dot = Vector3.Dot(Velocity, normal);
          if (dot < 0f)
            Velocity -= normal * dot;
        }
        if (collision.angle >= 135f && Velocity.y > 0f)
          Velocity.y = 0f;
      }
    }
  }
  
  private void HandleMovement()
  {
    if (isDryJump && !isGrounded)
    {
      Velocity.x = 0f;
      Velocity.z = 0f;
      return;
    }

    float speed, accel, decel;

    if (isGrounded || isOnSteepSlope)
    {
      speed = MovingSpeed;
      accel = Acceleration;
      decel = Deceleration;
    }
    else
    {
      speed = AirMovingSpeed;
      accel = AirAcceleration;
      decel = AirDeceleration;
    }

    Vector2 effectiveDirection = GetSmoothedDirection(cameraRelativeDirection);

    float targetX = effectiveDirection.x * speed;
    float targetZ = effectiveDirection.y * speed;

    if (isGrounded)
    {
      Vector3 horizontalVel = new Vector3(Velocity.x, 0f, Velocity.z);
      bool hasInput = effectiveDirection.sqrMagnitude > 0.01f;
      bool nearlyStill = horizontalVel.sqrMagnitude < MinStartSpeed * MinStartSpeed;

      if (hasInput && nearlyStill)
      {
        Vector3 inputDir = new Vector3(effectiveDirection.x, 0f, effectiveDirection.y).normalized;
        Velocity.x = inputDir.x * MinStartSpeed;
        Velocity.z = inputDir.z * MinStartSpeed;
      }
    }

    float rateX = GetMovementRate(Velocity.x, targetX, accel, decel);
    float rateZ = GetMovementRate(Velocity.z, targetZ, accel, decel);

    Velocity.x = Mathf.MoveTowards(Velocity.x, targetX, rateX * Time.fixedDeltaTime);
    Velocity.z = Mathf.MoveTowards(Velocity.z, targetZ, rateZ * Time.fixedDeltaTime);
  }

  private Vector2 GetSmoothedDirection(Vector2 rawDirection)
  {
    bool hasInput = rawDirection.sqrMagnitude > 0.01f;

    if (!hasInput)
    {
      Vector2 horizontalVel = new Vector2(Velocity.x, Velocity.z);
      if (horizontalVel.sqrMagnitude > 0.01f)
        smoothedDirection = horizontalVel.normalized;
      return rawDirection;
    }

    Vector2 moveDir = new Vector2(Velocity.x, Velocity.z);
    
    if (moveDir.sqrMagnitude < 0.01f)
    {
      smoothedDirection = rawDirection;
      return rawDirection;
    }

    float angle = Vector2.Angle(moveDir.normalized, rawDirection.normalized);

    if (angle >= ReversalAngle)
    {
      smoothedDirection = rawDirection;
      return rawDirection;
    }

    smoothedDirection = Vector2.Lerp(smoothedDirection, rawDirection, CurveTurnSpeed * Time.fixedDeltaTime);
    return smoothedDirection.normalized * rawDirection.magnitude;
  }

  private float GetMovementRate(float current, float target, float accel, float decel)
  {
    if (!isGrounded && Mathf.Abs(current) > AirMovingSpeed)
    {
      if (Mathf.Sign(current) == Mathf.Sign(target) || Mathf.Approximately(target, 0f))
        return AirDragDeceleration;
      return decel;
    }
    return IsApproachingTarget(current, target) ? accel : decel;
  }

  private bool IsApproachingTarget(float current, float target)
  {
    if (Mathf.Approximately(target, 0f)) return false;
    if (Mathf.Sign(current) != Mathf.Sign(target)) return false;
    if (Mathf.Abs(current) > Mathf.Abs(target)) return false;
    return true;
  }

  private void HandleGrounded()
  {
    GroundInfo ground = physics.CheckGround();
    isGrounded = ground.isGrounded;
    isOnSteepSlope = ground.isSteepSlope;
  }
  
  private void HandleDebug()
  {
    GroundedDebug = isGrounded;
    ActionDebug = isAction;
    JumpDebug = isJump;
    CrouchDebug = isCrouch;
    SwapDebug = isSwap;
    DirectionDebug = Direction;
    VelocityDebug = Velocity;
    speedDebug = new Vector2(Velocity.x,Velocity.z).magnitude;
    DryJumpDebug = isDryJump;
  }
}