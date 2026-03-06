using System;
using UnityEngine;

[RequireComponent(typeof(PhysicsController))]
public class PlayerController : MonoBehaviour //ADD STATE SYSTEM FOR ANIMATIONS
{
  //Requirements
  PhysicsController physics;
  
  [Header("Camera Settings")]
  public Transform CameraTransform;
  
  [Header("Gravity Settings")]
  //Configurable Constants
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
  public float CoyoteTime = 0.12f;        
  public float JumpBufferTime = 0.1f;     

  [Space(20)]
  [Header("Curve Turn Settings")]
  [Tooltip("Velocidad de interpolación de la dirección suavizada")]
  public float CurveTurnSpeed = 5f;
  [Tooltip("Ángulo a partir del cual se considera reversa total (sin suavizado)")]
  public float ReversalAngle = 150f;

  [Space(20)]
  [Header("Air Rotation Lock Settings")]
  [Tooltip("Ángulo entre la dirección de despegue y el input para bloquear rotación en aire")]
  public float AirReversalLockAngle = 120f;
  
  
  //Private Variables
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

  // Curve turn state
  private Vector2 smoothedDirection;

  // Air rotation lock state
  private bool airRotationLocked;
  private Quaternion lockedRotation;
  private Vector3 takeoffDirection;

  // Dry jump state
  private bool isDryJump;

  // Camera relative input
  private Vector2 cameraRelativeDirection;
  
  //Debug
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
  
  //Connect Physics
  void Awake() 
  {
    physics = GetComponent<PhysicsController>();
  }

  //Connect Input
  void Start()
  {
    if (CameraTransform == null && Camera.main != null)
      CameraTransform = Camera.main.transform;

    EventBus.Subscribe<OnActionInputEvent>(@event => isAction = @event.pressed);
    EventBus.Subscribe<OnCrouchInputEvent>(@event => isCrouch = @event.pressed);
    EventBus.Subscribe<OnJumpInputEvent>(@event => isJump = @event.pressed);
    EventBus.Subscribe<OnSwapInputEvent>(@event => isSwap = @event.pressed);
    EventBus.Subscribe<OnMoveInputEvent>(@event => Direction = @event.Direction);
  }


  //Do Update Stuff
  void Update()
  {
    // Input se captura via EventBus
  }

  void FixedUpdate()
  {
    HandleCameraRelativeInput();
    HandleGrounded();
    HandleGravity();
    HandleJump();
    HandleMovement();
    HandleRotation();
    HandleMotion();
    HandleDebug();
  }

  private void HandleCameraRelativeInput()
  {
    if (Direction.sqrMagnitude < 0.01f || CameraTransform == null)
    {
      cameraRelativeDirection = Vector2.zero;
      return;
    }

    // Obtener forward y right de la cámara, aplanados en Y
    Vector3 camForward = CameraTransform.forward;
    Vector3 camRight = CameraTransform.right;

    camForward.y = 0f;
    camRight.y = 0f;
    camForward.Normalize();
    camRight.Normalize();

    // Convertir input a dirección relativa a la cámara
    Vector3 worldDirection = camForward * Direction.y + camRight * Direction.x;

    cameraRelativeDirection = new Vector2(worldDirection.x, worldDirection.z);

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
      coyoteTimer = 0f;
      jumpBufferTimer = 0f;
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

    if (isDryJump)
      return;

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

 /* private void OnDrawGizmos()
  {
    if (!Application.isPlaying) return;

    Vector3 start = transform.position + Vector3.up * 2f;
    Vector3 end = start + Vector3.up * (Velocity.y * 0.2f);

    if (Velocity.y > 0f)
      Gizmos.color = Color.green;
    else if (Velocity.y < -5f)
      Gizmos.color = Color.red;
    else
      Gizmos.color = Color.yellow;

    Gizmos.DrawLine(start, end);
    Gizmos.DrawSphere(end, 0.1f);

#if UNITY_EDITOR
    UnityEditor.Handles.Label(start + Vector3.right * 0.3f, $"VelY: {Velocity.y:F2}");
#endif
  }*/
}