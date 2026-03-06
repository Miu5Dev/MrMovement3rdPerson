using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
  [Header("Target")]
  public Transform Target;

  [Space(20)]
  [Header("Distance Settings")]
  public float Distance = 6f;
  public float MinDistance = 2f;
  public float MaxDistance = 10f;

  [Space(20)]
  [Header("Height Settings")]
  [Tooltip("Offset vertical del punto al que mira la cámara")]
  public float LookAtHeight = 1.2f;

  [Space(20)]
  [Header("Sensitivity Settings")]
  public float HorizontalSensitivity = 150f;
  public float VerticalSensitivity = 100f;
  [Tooltip("Invertir eje Y")]
  public bool InvertY = false;

  [Space(20)]
  [Header("Vertical Clamp")]
  public float MinVerticalAngle = -20f;
  public float MaxVerticalAngle = 60f;

  [Space(20)]
  [Header("Collision")]
  public float CollisionRadius = 0.3f;
  public LayerMask CollisionMask = ~0;

  [Space(20)]
  [Header("Auto Rotation")]
  [Tooltip("Tiempo sin input de cámara para empezar a auto-rotar detrás del jugador")]
  public float AutoRotateDelay = 3f;
  [Tooltip("Velocidad de auto-rotación detrás del jugador")]
  public float AutoRotateSpeed = 2f;
  [Tooltip("Velocidad mínima del jugador para activar auto-rotación")]
  public float AutoRotateMinSpeed = 1f;

  // Private state
  private float yaw;
  private float pitch;
  private Vector2 lookInput;
  private float timeSinceLastLookInput;
  private PlayerController playerController;

  void Start()
  {
    if (Target == null)
    {
      GameObject player = GameObject.FindGameObjectWithTag("Player");
      if (player != null)
        Target = player.transform;
    }

    if (Target != null)
    {
      playerController = Target.GetComponent<PlayerController>();

      Vector3 lookAtPoint = Target.position + Vector3.up * LookAtHeight;
      Vector3 direction = transform.position - lookAtPoint;
      yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
      pitch = Mathf.Asin(direction.normalized.y) * Mathf.Rad2Deg;
      pitch = Mathf.Clamp(pitch, MinVerticalAngle, MaxVerticalAngle);
    }

    Cursor.lockState = CursorLockMode.Locked;
    Cursor.visible = false;

    EventBus.Subscribe<OnLookInputEvent>(OnLookInput);
  }

  void OnDestroy()
  {
    EventBus.Unsubscribe<OnLookInputEvent>(OnLookInput);
  }

  private void OnLookInput(OnLookInputEvent evt)
  {
    lookInput = evt.Delta;
  }

  void LateUpdate()
  {
    if (Target == null) return;

    HandleInput();
    HandleAutoRotation();
    HandlePosition();
  }

  private void HandleInput()
  {
    bool hasInput = lookInput.sqrMagnitude > 0.01f;

    if (hasInput)
    {
      float invertMultiplier = InvertY ? 1f : -1f;

      yaw += lookInput.x * HorizontalSensitivity * Time.deltaTime;
      pitch += lookInput.y * VerticalSensitivity * invertMultiplier * Time.deltaTime;
      pitch = Mathf.Clamp(pitch, MinVerticalAngle, MaxVerticalAngle);

      timeSinceLastLookInput = 0f;
    }
    else
    {
      timeSinceLastLookInput += Time.deltaTime;
    }
  }

  private void HandleAutoRotation()
  {
    if (timeSinceLastLookInput < AutoRotateDelay) return;
    if (playerController == null) return;

    Vector3 horizontalVelocity = new Vector3(playerController.VelocityDebug.x, 0f, playerController.VelocityDebug.z);

    if (horizontalVelocity.sqrMagnitude < AutoRotateMinSpeed * AutoRotateMinSpeed) return;

    float targetYaw = Mathf.Atan2(horizontalVelocity.x, horizontalVelocity.z) * Mathf.Rad2Deg;

    yaw = Mathf.LerpAngle(yaw, targetYaw, AutoRotateSpeed * Time.deltaTime);
  }

  private void HandlePosition()
  {
    Vector3 lookAtPoint = Target.position + Vector3.up * LookAtHeight;

    Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
    Vector3 offset = rotation * new Vector3(0f, 0f, -Distance);
    Vector3 desiredPosition = lookAtPoint + offset;

    float actualDistance = Distance;
    Vector3 direction = desiredPosition - lookAtPoint;

    if (Physics.SphereCast(lookAtPoint, CollisionRadius, direction.normalized, out RaycastHit hit, Distance, CollisionMask))
    {
      actualDistance = hit.distance - CollisionRadius;
      actualDistance = Mathf.Max(actualDistance, MinDistance);
    }

    Vector3 finalOffset = rotation * new Vector3(0f, 0f, -actualDistance);
    transform.position = lookAtPoint + finalOffset;
    transform.LookAt(lookAtPoint);
  }

  private void OnDrawGizmosSelected()
  {
    if (Target == null) return;

    Vector3 lookAt = Target.position + Vector3.up * LookAtHeight;
    Gizmos.color = Color.cyan;
    Gizmos.DrawWireSphere(lookAt, 0.2f);

    Gizmos.color = Color.yellow;
    Gizmos.DrawLine(transform.position, lookAt);

    Gizmos.color = Color.red;
    Gizmos.DrawWireSphere(transform.position, CollisionRadius);
  }
}