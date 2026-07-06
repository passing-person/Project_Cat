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
    private Collider[] ownColliders;
    private Vector3 moveInput;
    private bool isSprinting;
    private float lastGroundedTime = -999f;

    private const float GroundContactGraceTime = 0.12f;

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
        ownColliders = GetComponentsInChildren<Collider>();
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
        lastGroundedTime = -999f;
        animationController?.PlayJump();
        sfxController?.PlayJump();
    }

    private bool IsGrounded()
    {
        if (Time.time - lastGroundedTime <= GroundContactGraceTime)
        {
            return true;
        }

        Vector3 origin = groundCheck != null ? groundCheck.position : transform.position + Vector3.up * 0.05f;
        Collider[] groundHits = Physics.OverlapSphere(origin, groundCheckRadius, groundLayer, QueryTriggerInteraction.Ignore);
        foreach (Collider hit in groundHits)
        {
            if (!IsOwnCollider(hit))
            {
                lastGroundedTime = Time.time;
                return true;
            }
        }

        float rayDistance = groundRayLength + groundCheckRadius + 0.08f;
        RaycastHit[] rayHits = Physics.RaycastAll(origin + Vector3.up * groundCheckRadius, Vector3.down, rayDistance, groundLayer, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit rayHit in rayHits)
        {
            if (!IsOwnCollider(rayHit.collider))
            {
                lastGroundedTime = Time.time;
                return true;
            }
        }

        return false;
    }

    private void OnCollisionStay(Collision collision)
    {
        if (IsOwnCollider(collision.collider))
        {
            return;
        }

        if ((groundLayer.value & (1 << collision.gameObject.layer)) == 0)
        {
            return;
        }

        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0.45f)
            {
                lastGroundedTime = Time.time;
                return;
            }
        }
    }

    private bool IsOwnCollider(Collider target)
    {
        if (target == null || ownColliders == null)
        {
            return false;
        }

        foreach (Collider ownCollider in ownColliders)
        {
            if (ownCollider == target)
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateAnimation(bool moving, bool grounded)
    {
        if (animationController == null)
        {
            return;
        }

        float animationSpeed = moving ? CurrentMoveSpeed : 0f;
        float localX = 0f;
        float localY = 0f;
        if (moving)
        {
            float forwardAmount = Vector3.Dot(transform.forward, moveInput);
            float signedForward = Mathf.Abs(forwardAmount) > 0.1f ? Mathf.Sign(forwardAmount) : 1f;
            localY = signedForward * animationSpeed;
        }

        animationController.SetMoveSpeed(animationSpeed);
        animationController.SetMoveDirection(localX, localY);
        animationController.SetGrounded(grounded);
    }
}
