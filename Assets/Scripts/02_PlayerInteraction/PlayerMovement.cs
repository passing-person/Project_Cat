using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerAnimationController animationController;
    [SerializeField] private PlayerSfxController sfxController;
    [SerializeField] private Transform groundCheck;

    [Header("Movement (moveSpeed in debug)")]
    private readonly float moveSpeed = 1.333333f;

    [Header("Jump (jumpForce in debug)")]
    private readonly float jumpForce = 3.96f;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    private Rigidbody rb;
    private Vector3 moveInput;

    public bool IsMoving => moveInput.sqrMagnitude > 0.01f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (playerController == null) playerController = GetComponent<PlayerController>();
        if (animationController == null) animationController = GetComponent<PlayerAnimationController>();
        if (sfxController == null) sfxController = GetComponent<PlayerSfxController>();
    }

    private void Update()
    {
        if (!CanMove())
        {
            moveInput = Vector3.zero;
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

        if (Input.GetKeyDown(KeyCode.Space))
            TryJump();

        UpdateAnimation(IsMoving, IsGrounded());
    }

    private void FixedUpdate()
    {
        if (!CanMove())
            return;

        Vector3 velocity = moveInput * moveSpeed;
        Vector3 nextPosition = rb.position + velocity * Time.fixedDeltaTime;
        rb.MovePosition(nextPosition);
    }

    public void EnableMovement()
    {
        if (playerController != null)
            playerController.SetControllable(true);
    }

    public void DisableMovement()
    {
        if (playerController != null)
            playerController.SetControllable(false);
    }

    private bool CanMove()
    {
        if (playerController == null)
            return true;

        return playerController.IsControllable && !playerController.IsHidden;
    }

    private void TryJump()
    {
        if (!IsGrounded())
            return;



        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

        animationController?.PlayJump();
        sfxController?.PlayJump();
    }

    private bool IsGrounded()
    {
        if (groundCheck == null)
            return false;

        float rayLength = 0.1f;
        RaycastHit hit;
        return Physics.Raycast(groundCheck.position, Vector3.down, out hit, rayLength, groundLayer);
}

    private void UpdateAnimation(bool isMoving, bool isGrounded)
    {
        if (animationController == null)
            return;

        animationController.SetMoveSpeed(isMoving ? moveSpeed : 0f);
        animationController.SetGrounded(isGrounded);
    }
}
