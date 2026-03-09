using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform Target;
    public float Distance = 6f;
    public float MinDistance = 2f;
    public float LookAtHeight = 1.2f;
    public float CollisionRadius = 0.3f;
    public LayerMask CollisionMask = ~0;

    [Space(10)]
    public float MouseSensX = 3f;
    public float MouseSensY = 2f;
    public float PadSensX = 150f;
    public float PadSensY = 100f;
    public bool InvertY = false;
    public float MinPitch = -20f;
    public float MaxPitch = 60f;

    [Space(10)]
    public float YawFollowSpeed = 1.5f;
    public float YawFollowMinSpeed = 1f;

    [Space(10)]
    [Tooltip("La cámara se queda atrás cuando el jugador corre, dando sensación de velocidad")]
    public float SpeedLagAmount = 1.5f;
    [Tooltip("Velocidad mínima para activar el lag")]
    public float SpeedLagMinSpeed = 2f;
    [Tooltip("Suavizado del lag")]
    public float SpeedLagSmooth = 3f;

    private float yaw;
    private float pitch;
    private Vector2 lookInput;
    private LookInputSource lookSource;
    private PlayerController playerController;
    private float smoothDist;
    private Vector3 currentLag;

    void Start()
    {
        if (Target == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) Target = p.transform;
        }

        if (Target != null)
        {
            playerController = Target.GetComponent<PlayerController>();
            smoothDist = Distance;
            currentLag = Vector3.zero;
            Vector3 dir = transform.position - (Target.position + Vector3.up * LookAtHeight);
            yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            pitch = Mathf.Asin(Mathf.Clamp(dir.normalized.y, -1f, 1f)) * Mathf.Rad2Deg;
            pitch = Mathf.Clamp(pitch, MinPitch, MaxPitch);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        EventBus.Subscribe<OnLookInputEvent>(e => { lookInput = e.Delta; lookSource = e.Source; });
    }

    void LateUpdate()
    {
        if (Target == null) return;

        float dt = Time.deltaTime;
        if (dt < 0.0001f) return;

        // Target.position ya está interpolado si el Rigidbody tiene Interpolate
        Vector3 targetPos = Target.position;

        // ---- INPUT ----
        if (lookInput.sqrMagnitude > 0.01f)
        {
            float inv = InvertY ? 1f : -1f;
            if (lookSource == LookInputSource.Gamepad)
            {
                yaw += lookInput.x * PadSensX * dt;
                pitch += lookInput.y * PadSensY * inv * dt;
            }
            else
            {
                yaw += lookInput.x * MouseSensX;
                pitch += lookInput.y * MouseSensY * inv;
            }
            pitch = Mathf.Clamp(pitch, MinPitch, MaxPitch);
        }

        // ---- AUTO YAW ----
        if (playerController != null)
        {
            Vector3 hVel = new Vector3(
                playerController.VelocityDebug.x, 0f, playerController.VelocityDebug.z);

            float hSpeed = hVel.magnitude;

            if (hSpeed > YawFollowMinSpeed)
            {
                Vector3 camToPlayer = targetPos - transform.position;
                camToPlayer.y = 0f;

                float dot = 0f;
                if (camToPlayer.sqrMagnitude > 0.1f)
                    dot = Vector3.Dot(hVel.normalized, camToPlayer.normalized);

                if (dot > -0.5f)
                {
                    float targetYaw = Mathf.Atan2(hVel.x, hVel.z) * Mathf.Rad2Deg;
                    yaw = Mathf.LerpAngle(yaw, targetYaw, YawFollowSpeed * dt);
                }
            }
        }

        // ---- SPEED LAG ----
        // La cámara se queda ATRÁS cuando el jugador se mueve rápido.
        // El offset es en la dirección OPUESTA a la velocidad.
        Vector3 targetLag = Vector3.zero;
        if (playerController != null)
        {
            Vector3 hVel = new Vector3(
                playerController.VelocityDebug.x, 0f, playerController.VelocityDebug.z);
            float hSpeed = hVel.magnitude;

            if (hSpeed > SpeedLagMinSpeed)
            {
                float factor = Mathf.Clamp01(
                    (hSpeed - SpeedLagMinSpeed) /
                    (playerController.MovingSpeed - SpeedLagMinSpeed + 0.01f));
                // Opuesto a la velocidad = la cámara se queda atrás
                targetLag = -(hVel / hSpeed) * SpeedLagAmount * factor;
            }
        }
        currentLag = Vector3.Lerp(currentLag, targetLag,
            1f - Mathf.Exp(-SpeedLagSmooth * dt));

        // ---- POSICIÓN ----
        Vector3 center = targetPos + Vector3.up * LookAtHeight + currentLag;

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 back = rot * Vector3.back;

        float desiredDist = Distance;
        if (Physics.SphereCast(
            targetPos + Vector3.up * LookAtHeight,
            CollisionRadius, back,
            out RaycastHit hit, Distance, CollisionMask))
        {
            desiredDist = Mathf.Max(hit.distance - CollisionRadius, MinDistance);
        }

        float distSpeed = desiredDist < smoothDist ? 15f : 5f;
        smoothDist = Mathf.Lerp(smoothDist, desiredDist, 1f - Mathf.Exp(-distSpeed * dt));

        transform.position = center + back * smoothDist;
        transform.rotation = rot;
    }
}