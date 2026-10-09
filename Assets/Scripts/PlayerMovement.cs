using UnityEngine;

public enum MoveState { Idle, Crouching, Walking, Sprinting }

/// <summary>
/// First-person movement for Marginalia.
/// W A S D move, Shift sprints (loud), Ctrl crouches (very quiet).
/// Emits noise events the Curator can hear.
/// Setup: put on the Player object (with a CharacterController).
/// Assign cameraRoot to the camera (or an empty parent of it).
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Speeds (m/s)")]
    [SerializeField] float crouchSpeed = 1.5f;
    [SerializeField] float walkSpeed = 3.0f;
    [SerializeField] float sprintSpeed = 5.5f;
    [Tooltip("How fast we reach the target speed. Lower = heavier feel.")]
    [SerializeField] float acceleration = 14f;

    [Header("Crouch")]
    [SerializeField] float standingHeight = 1.8f;
    [SerializeField] float crouchHeight = 1.0f;
    [SerializeField] float standingEyeHeight = 1.6f;
    [SerializeField] float crouchEyeHeight = 0.8f;
    [SerializeField] float crouchTransitionSpeed = 8f;
    [SerializeField] LayerMask ceilingMask = ~0;
    [SerializeField] Transform cameraRoot;

    [Header("Gravity")]
    [SerializeField] float gravity = -20f;

    [Header("Noise")]
    [Tooltip("Seconds between noise events while moving.")]
    [SerializeField] float noiseInterval = 0.3f;

    // --- Read by other systems (HUD noise meter, Curator, etc.) ---
    public MoveState State { get; private set; } = MoveState.Idle;
    public float CurrentNoise { get; private set; }
    public bool IsCrouching { get; private set; }

    CharacterController controller;
    Vector3 horizontalVelocity;
    float verticalVelocity;
    float noiseTimer;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        controller.height = standingHeight;
        controller.center = new Vector3(0f, standingHeight / 2f, 0f); // pivot at feet
    }

    void Update()
    {
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        input = Vector2.ClampMagnitude(input, 1f);
        bool hasInput = input.sqrMagnitude > 0.01f;

        HandleCrouch();

        // Sprint only when moving, not crouching, and not walking backwards
        bool wantsSprint = Input.GetKey(KeyCode.LeftShift) && !IsCrouching && input.y > 0.1f;

        float targetSpeed = IsCrouching ? crouchSpeed : (wantsSprint ? sprintSpeed : walkSpeed);
        Vector3 wishDir = transform.TransformDirection(new Vector3(input.x, 0f, input.y));
        Vector3 targetVelocity = hasInput ? wishDir * targetSpeed : Vector3.zero;
        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, acceleration * Time.deltaTime);

        // Gravity
        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;

        Vector3 motion = horizontalVelocity + Vector3.up * verticalVelocity;
        controller.Move(motion * Time.deltaTime);

        UpdateStateAndNoise(hasInput, wantsSprint);
    }

    void HandleCrouch()
    {
        bool wantsCrouch = Input.GetKey(KeyCode.LeftControl);

        if (wantsCrouch) IsCrouching = true;
        else if (IsCrouching && CanStandUp()) IsCrouching = false;

        float targetHeight = IsCrouching ? crouchHeight : standingHeight;
        float targetEye = IsCrouching ? crouchEyeHeight : standingEyeHeight;

        controller.height = Mathf.MoveTowards(controller.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);
        controller.center = new Vector3(0f, controller.height / 2f, 0f);

        if (cameraRoot != null)
        {
            Vector3 p = cameraRoot.localPosition;
            p.y = Mathf.MoveTowards(p.y, targetEye, crouchTransitionSpeed * Time.deltaTime);
            cameraRoot.localPosition = p;
        }
    }

    // Don't let the player stand up inside a low ceiling (e.g. under shelves).
    bool CanStandUp()
    {
        float r = controller.radius * 0.9f;
        Vector3 origin = transform.position + Vector3.up * (controller.height - controller.radius);
        float distance = standingHeight - controller.height + 0.05f;
        return !Physics.SphereCast(origin, r, Vector3.up, out _, distance, ceilingMask, QueryTriggerInteraction.Ignore);
    }

    void UpdateStateAndNoise(bool hasInput, bool wantsSprint)
    {
        bool moving = hasInput && horizontalVelocity.sqrMagnitude > 0.05f;

        if (!moving)
        {
            State = IsCrouching ? MoveState.Crouching : MoveState.Idle;
            CurrentNoise = NoiseLevels.Silent;
            noiseTimer = 0f;
            return;
        }

        if (IsCrouching)
        {
            State = MoveState.Crouching;
            CurrentNoise = NoiseLevels.CrouchWalk;
        }
        else if (wantsSprint)
        {
            State = MoveState.Sprinting;
            CurrentNoise = NoiseLevels.Sprint;
        }
        else
        {
            State = MoveState.Walking;
            CurrentNoise = NoiseLevels.Walk;
        }

        noiseTimer -= Time.deltaTime;
        if (noiseTimer <= 0f)
        {
            NoiseSystem.Emit(transform.position, CurrentNoise, gameObject);
            noiseTimer = noiseInterval;
        }
    }
}