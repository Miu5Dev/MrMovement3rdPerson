using UnityEngine;

public enum PlayerAnimState
{
    Idle,
    MoveForward,
    MoveForwardRight,
    MoveForwardLeft,
    Jump,
    Fall
}

[RequireComponent(typeof(PlayerController))]
public class AnimationController : MonoBehaviour
{
    private PlayerController controller;

    [Header("Settings")]
    public Animator animator;
    public Transform CameraTransform;

    [Header("Thresholds")]
    public float MoveThreshold = 1.6f;
    public float SideAngle = 30f;
    public float BackAngle = 135f;

    [Header("Debug")]
    public PlayerAnimState CurrentState;

    private PlayerAnimState previousState;

    private static readonly int AnimState = Animator.StringToHash("AnimState");
    private static readonly int Speed = Animator.StringToHash("Speed");
    private static readonly int IsGrounded = Animator.StringToHash("IsGrounded");

    void Awake()
    {
        controller = GetComponent<PlayerController>();
    }

    void Start()
    {
        if (CameraTransform == null && Camera.main != null)
            CameraTransform = Camera.main.transform;

        CurrentState = PlayerAnimState.Idle;
        previousState = PlayerAnimState.Idle;
    }

    void Update()
    {
        CurrentState = EvaluateState();

        if (animator == null) return;

        if (CurrentState != previousState)
        {
            animator.SetInteger(AnimState, (int)CurrentState);
            previousState = CurrentState;
        }

        animator.SetFloat(Speed, controller.speedDebug);
        animator.SetBool(IsGrounded, controller.GroundedDebug);
    }

    private PlayerAnimState EvaluateState()
    {
        bool grounded = controller.GroundedDebug;
        float speed = controller.speedDebug;
        Vector3 velocity = controller.VelocityDebug;

        if (!grounded)
        {
            if (velocity.y > 0.1f)
                return PlayerAnimState.Jump;
            else
                return PlayerAnimState.Fall;
        }

        if (speed < MoveThreshold)
            return PlayerAnimState.Idle;

        Vector3 hVel = new Vector3(velocity.x, 0f, velocity.z);
        if (hVel.sqrMagnitude < 0.01f)
            return PlayerAnimState.Idle;

        if (CameraTransform == null)
            return PlayerAnimState.MoveForward;

        Vector3 camForward = transform.position - CameraTransform.position;
        camForward.y = 0f;
        if (camForward.sqrMagnitude < 0.01f)
            return PlayerAnimState.MoveForward;

        camForward.Normalize();

        float angle = Vector3.SignedAngle(camForward, hVel.normalized, Vector3.up);
        float absAngle = Mathf.Abs(angle);

        // Backward o Forward → misma animación
        if (absAngle > BackAngle || absAngle < SideAngle)
            return PlayerAnimState.MoveForward;

        if (angle > 0f)
            return PlayerAnimState.MoveForwardRight;
        else
            return PlayerAnimState.MoveForwardLeft;
    }
}