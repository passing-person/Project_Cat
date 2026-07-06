using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerAnimationController animationController;
    [SerializeField] private PlayerSfxController sfxController;
    [SerializeField] private Transform groundCheck;

    [Header("Movement")]
    public float moveSpeed = 2.4f;
    public float sprintMultiplier = 1.55f;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public float movementAcceleration = 16f;
    public float movementDeceleration = 22f;

    [Header("Idle Turn Animation")]
    public bool enableIdleTurnAnimation = true;
    public float idleTurnTriggerAngle = 32f;
    public float idleTurnCooldown = 0.35f;

    [Header("Jump")]
    public float jumpForce = 4.2f;
    public float groundCheckRadius = 0.22f;
    public float groundRayLength = 0.18f;
    public float groundCheckDelayAfterJump = 0.12f;
    public LayerMask groundLayer = ~0;

    private Rigidbody rb;
    private Vector3 moveInput;
    private Vector3 desiredMoveInput;
    private bool isSprinting;
    private float ignoreGroundedUntil;
    private float previousYaw;
    private float idleYawAccumulator;
    private float nextIdleTurnTime;

    public bool IsMoving => moveInput.sqrMagnitude > 0.01f;
    public bool IsSprinting => isSprinting;
    public float CurrentMoveSpeed => isSprinting ? moveSpeed * sprintMultiplier : moveSpeed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (playerController == null) playerController = GetComponent<PlayerController>();
        if (animationController == null) animationController = GetComponent<PlayerAnimationController>();
        if (sfxController == null) sfxController = GetComponent<PlayerSfxController>();

        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        previousYaw = transform.eulerAngles.y;
    }

    private void Update()
    {
        if (!CanMove())
        {
            desiredMoveInput = Vector3.zero;
            moveInput = Vector3.zero;
            isSprinting = false;
            previousYaw = transform.eulerAngles.y;
            idleYawAccumulator = 0f;
            UpdateAnimation(false, IsGrounded());
            return;
        }

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = transform.right;
        right.y = 0f;
        right.Normalize();

        desiredMoveInput = (right * horizontal + forward * vertical).normalized;

        float smoothing = desiredMoveInput.sqrMagnitude > 0.01f ? movementAcceleration : movementDeceleration;
        moveInput = Vector3.MoveTowards(moveInput, desiredMoveInput, smoothing * Time.deltaTime);
        isSprinting = Input.GetKey(sprintKey) && desiredMoveInput.sqrMagnitude > 0.01f;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            TryJump();
        }

        UpdateIdleTurnAnimation();
        UpdateAnimation(IsMoving, IsGrounded());
    }

    private void FixedUpdate()
    {
        if (!CanMove())
        {
            return;
        }

        Vector3 velocity = moveInput * CurrentMoveSpeed;
        Vector3 nextPosition = rb.position + velocity * Time.fixedDeltaTime;
        rb.MovePosition(nextPosition);
    }

    public void EnableMovement()
    {
        playerController?.SetControllable(true);
    }

    public void DisableMovement()
    {
        playerController?.SetControllable(false);
    }

    private bool CanMove()
    {
        if (playerController == null)
        {
            return true;
        }

        return playerController.IsControllable && !playerController.IsHidden;
    }

    private void TryJump()
    {
        if (!IsGrounded())
        {
            return;
        }

        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        ignoreGroundedUntil = Time.time + groundCheckDelayAfterJump;
        animationController?.SetGrounded(false);
        animationController?.PlayJump();
        sfxController?.PlayJump();
    }

    private bool IsGrounded()
    {
        if (Time.time < ignoreGroundedUntil)
        {
            return false;
        }

        Vector3 origin = groundCheck != null ? groundCheck.position : transform.position + Vector3.up * 0.05f;
        if (Physics.CheckSphere(origin, groundCheckRadius, groundLayer, QueryTriggerInteraction.Ignore))
        {
            return true;
        }

        return Physics.Raycast(origin, Vector3.down, groundRayLength + 0.08f, groundLayer, QueryTriggerInteraction.Ignore);
    }

    private void UpdateAnimation(bool moving, bool grounded)
    {
        if (animationController == null)
        {
            return;
        }

        animationController.SetMoveSpeed(moving ? CurrentMoveSpeed : 0f);
        if (moving)
        {
            Vector3 localMove = transform.InverseTransformDirection(moveInput);
            animationController.SetMoveDirection(localMove.x * CurrentMoveSpeed, localMove.z * CurrentMoveSpeed);
        }
        else
        {
            animationController.SetMoveDirection(0f, 0f);
        }

        animationController.SetGrounded(grounded);
    }

    private void UpdateIdleTurnAnimation()
    {
        float currentYaw = transform.eulerAngles.y;
        float yawDelta = Mathf.DeltaAngle(previousYaw, currentYaw);
        previousYaw = currentYaw;

        if (animationController == null)
        {
            return;
        }

        if (!enableIdleTurnAnimation)
        {
            idleYawAccumulator = 0f;
            return;
        }

        if (desiredMoveInput.sqrMagnitude > 0.01f || moveInput.sqrMagnitude > 0.01f)
        {
            idleYawAccumulator = 0f;
            return;
        }

        if (Time.time < nextIdleTurnTime)
        {
            return;
        }

        idleYawAccumulator += yawDelta;
        if (Mathf.Abs(idleYawAccumulator) < idleTurnTriggerAngle)
        {
            return;
        }

        if (idleYawAccumulator > 0f)
        {
            animationController.PlayTurnRight();
        }
        else
        {
            animationController.PlayTurnLeft();
        }

        idleYawAccumulator = 0f;
        nextIdleTurnTime = Time.time + idleTurnCooldown;
    }
}
