using System;
using UnityEngine;

[RequireComponent(typeof(PhysicsController))]
public class PlayerController : MonoBehaviour
{
  //Requirements
  PhysicsController physics;
  
  //Configurable Constants
  public float Gravity = -9.81f;
  [Space(7)]
  public float MovingSpeed = 12f;
  public float MinStartSpeed = 3f;
  public float Acceleration = 10f;
  public float Deceleration = 15f;

  public float AirMovingSpeed = 4f;
  public float AirAcceleration = 2f;      
  public float AirDeceleration = 1.5f;     
  public float AirDragDeceleration = 0.8f;
  
  public float RotationSpeed = 15f;
  public float AirRotationSpeed = 8f;
  
  //Private Variables
  private Vector3 Velocity;
  private bool isGrounded;
  
  private bool isOnSteepSlope;

  private bool isAction;
  private bool isJump;
  private bool isCrouch;
  private bool isSwap;
  private Vector2 Direction;
  
  //Debug
  [Space(20)]
  [Header("DEBUG")] 
  public bool GroundedDebug = false;
  public bool ActionDebug = false;
  public bool JumpDebug = false;
  public bool CrouchDebug = false;
  public bool SwapDebug = false;
  public Vector2 DirectionDebug;
  [Space(5)]
  public Vector3 VelocityDebug;
  
  //Connect Physics
  void Awake() 
  {
    physics = GetComponent<PhysicsController>();
  }

  //Connect Input
  void Start()
  {
    EventBus.Subscribe<OnActionInputEvent>(@event => isAction = @event.pressed);
    EventBus.Subscribe<OnCrouchInputEvent>(@event => isCrouch = @event.pressed);
    EventBus.Subscribe<OnJumpInputEvent>(@event => isJump = @event.pressed);
    EventBus.Subscribe<OnSwapInputEvent>(@event => isSwap = @event.pressed);
    EventBus.Subscribe<OnMoveInputEvent>(@event => Direction = @event.Direction);
  }


  //Do Update Stuff
  void Update()
  {
    HandleGrounded();
    HandleGravity();
    HandleMovement();
    HandleRotation();
    HandleMotion();
    HandleDebug();
  }
  
  private void HandleRotation()
  {
    // Solo rotar si hay movimiento horizontal
    Vector3 horizontalVelocity = new Vector3(Velocity.x, 0f, Velocity.z);
    if (horizontalVelocity.sqrMagnitude < 0.01f) return;

    Quaternion targetRotation = Quaternion.LookRotation(horizontalVelocity.normalized);
    float speed = isGrounded ? RotationSpeed : AirRotationSpeed;

    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, speed * Time.deltaTime);
  }
  
  private void HandleMotion()
  {
    MoveResult result = physics.Move(Velocity * Time.deltaTime);
    
    if (result.collided)
    {
      foreach (var collision in result.collisions)
      {
        if (collision.angle > 80f && collision.angle < 135f)
        {
          // Proyectar velocidad para remover solo el componente hacia la pared
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

    float targetX = Direction.x * speed;
    float targetZ = Direction.y * speed;

    // Impulso mínimo al empezar a moverte
    if (isGrounded)
    {
      Vector3 horizontalVel = new Vector3(Velocity.x, 0f, Velocity.z);
      bool hasInput = Direction.sqrMagnitude > 0.01f;
      bool nearlyStill = horizontalVel.sqrMagnitude < MinStartSpeed * MinStartSpeed;

      if (hasInput && nearlyStill)
      {
        Vector3 inputDir = new Vector3(Direction.x, 0f, Direction.y).normalized;
        Velocity.x = inputDir.x * MinStartSpeed;
        Velocity.z = inputDir.z * MinStartSpeed;
      }
    }

    float rateX = GetMovementRate(Velocity.x, targetX, accel, decel);
    float rateZ = GetMovementRate(Velocity.z, targetZ, accel, decel);

    Velocity.x = Mathf.MoveTowards(Velocity.x, targetX, rateX * Time.deltaTime);
    Velocity.z = Mathf.MoveTowards(Velocity.z, targetZ, rateZ * Time.deltaTime);
  }

  private float GetMovementRate(float current, float target, float accel, float decel)
  {
    // Caso especial: en el aire con momentum del suelo que excede la velocidad aérea
    // Se reduce muy lentamente para conservar momentum
    if (!isGrounded && Mathf.Abs(current) > AirMovingSpeed)
    {
      // Si el input va en la misma dirección que el momentum, 
      // solo aplica el drag suave para bajar al límite aéreo
      if (Mathf.Sign(current) == Mathf.Sign(target) || Mathf.Approximately(target, 0f))
        return AirDragDeceleration;

      // Si intenta cambiar de dirección contra el momentum, usa decel (aún lento en aire)
      return decel;
    }

    return IsApproachingTarget(current, target) ? accel : decel;
  }

// True si la velocidad actual va en la misma dirección que el target y no lo supera
  private bool IsApproachingTarget(float current, float target)
  {
    // Si no hay input, estamos desacelerando
    if (Mathf.Approximately(target, 0f)) return false;
    // Si tienen signos opuestos, estamos frenando para cambiar dirección
    if (Mathf.Sign(current) != Mathf.Sign(target)) return false;
    // Si ya superamos el target, desaceleramos
    if (Mathf.Abs(current) > Mathf.Abs(target)) return false;
    return true;
  }

  private void HandleGravity()
  {
    if(isGrounded) 
      Velocity.y = -2f;
    else
    {
      Velocity.y += Gravity * Time.deltaTime;
    }
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
  }
  
  
}
