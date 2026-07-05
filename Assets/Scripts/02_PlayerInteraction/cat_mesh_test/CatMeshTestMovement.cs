using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class CatMeshTestMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 1.25f;
    [SerializeField] private float turnSpeed = 12f;
    [SerializeField] private Transform movementReference;
    [SerializeField] private bool lockIdleHorizontalMotion = true;

    [Header("Jump")]
    [SerializeField] private float jumpHeight = 1f;
    [SerializeField] private float groundCheckDistance = 0.15f;
    [SerializeField] private LayerMask groundLayer = ~0;

    [Header("Collider")]
    [SerializeField] private bool fitColliderOnStart = true;
    [SerializeField] private float capsuleRadius = 0.12f;
    [SerializeField] private float capsuleHeight = 0.32f;

    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private Vector3 moveInput;
    private bool hasMoveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        ConfigureBody();

        if (fitColliderOnStart)
        {
            FitColliderToRenderer();
        }
    }

    private void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        moveInput = GetReferencedMoveInput(horizontal, vertical);
        hasMoveInput = moveInput.sqrMagnitude > 0.01f;

        if (Input.GetKeyDown(KeyCode.Space) && IsGrounded())
        {
            Vector3 velocity = rb.velocity;
            velocity.y = Mathf.Max(0f, velocity.y);
            rb.velocity = velocity;
            rb.AddForce(Vector3.up * GetJumpVelocityForHeight(), ForceMode.VelocityChange);
        }
    }

    private void FixedUpdate()
    {
        if (!hasMoveInput)
        {
            LockHorizontalMotionIfNeeded();
            return;
        }

        Vector3 velocity = moveInput * moveSpeed;
        rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);

        Quaternion targetRotation = Quaternion.LookRotation(moveInput, Vector3.up);
        Quaternion nextRotation = Quaternion.Slerp(rb.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime);
        rb.MoveRotation(nextRotation);
    }

    private void ConfigureBody()
    {
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.useGravity = true;

        capsuleCollider.direction = 1;
        capsuleCollider.radius = capsuleRadius;
        capsuleCollider.height = capsuleHeight;
        capsuleCollider.center = new Vector3(0f, capsuleHeight * 0.5f, 0f);
    }

    private void FitColliderToRenderer()
    {
        Renderer renderer = GetComponentInChildren<Renderer>();
        if (renderer == null)
        {
            return;
        }

        Bounds bounds = renderer.bounds;
        Vector3 localCenter = transform.InverseTransformPoint(bounds.center);
        capsuleCollider.height = Mathf.Max(0.05f, bounds.size.y);
        capsuleCollider.radius = Mathf.Max(0.03f, Mathf.Max(bounds.size.x, bounds.size.z) * 0.35f);
        capsuleCollider.center = new Vector3(localCenter.x, localCenter.y, localCenter.z);
    }

    private Vector3 GetReferencedMoveInput(float horizontal, float vertical)
    {
        Vector3 rawInput = new Vector3(horizontal, 0f, vertical);
        if (rawInput.sqrMagnitude <= 0.01f)
        {
            return Vector3.zero;
        }

        Transform reference = movementReference;
        if (reference == null && Camera.main != null)
        {
            reference = Camera.main.transform;
        }

        if (reference == null)
        {
            reference = transform;
        }

        Vector3 forward = reference.forward;
        Vector3 right = reference.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        return (right * horizontal + forward * vertical).normalized;
    }

    private bool IsGrounded()
    {
        const float footProbeStartHeight = 0.08f;
        Vector3 origin = transform.position + Vector3.up * footProbeStartHeight;
        return Physics.Raycast(origin, Vector3.down, footProbeStartHeight + groundCheckDistance, groundLayer, QueryTriggerInteraction.Ignore);
    }

    private float GetJumpVelocityForHeight()
    {
        return Mathf.Sqrt(Mathf.Max(0f, jumpHeight) * -2f * Physics.gravity.y);
    }

    private void LockHorizontalMotionIfNeeded()
    {
        if (!lockIdleHorizontalMotion)
        {
            return;
        }

        Vector3 velocity = rb.velocity;
        velocity.x = 0f;
        velocity.z = 0f;
        rb.velocity = velocity;
        rb.angularVelocity = Vector3.zero;
    }
}
