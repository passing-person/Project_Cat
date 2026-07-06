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

    [Header("Jump")]
    public float jumpForce = 4.2f;
    public float groundCheckRadius = 0.22f;
    public float groundRayLength = 0.18f;
    public LayerMask groundLayer = ~0;

    private Rigidbody rb;
    private Vector3 moveInput;
    private bool isSprinting;

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
    }

    private void Update()
    {
        if (!CanMove())
        {
            moveInput = Vector3.zero;
            isSprinting = false;
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

        moveInput = (right * horizontal + forward * vertical).normalized;
        isSprinting = Input.GetKey(sprintKey) && moveInput.sqrMagnitude > 0.01f;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            TryJump();
        }

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
        if (GameInputGate.IsGameplayInputBlocked)
        {
            return false;
        }

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
        animationController?.PlayJump();
        sfxController?.PlayJump();
    }

    private bool IsGrounded()
    {
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
        animationController.SetGrounded(grounded);
    }
}
