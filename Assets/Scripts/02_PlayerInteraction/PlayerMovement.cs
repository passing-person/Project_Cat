using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody rb;
    private Vector3 moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (playerController != null && !playerController.IsControllable)
        {
            moveInput = Vector3.zero;
            return;
        }

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        moveInput = new Vector3(horizontal, 0f, vertical).normalized;

        if (Input.GetKeyDown(KeyCode.Space))
            TryJump();
    }

    private void FixedUpdate()
    {
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

    private void TryJump()
    {
        if (!IsGrounded())
            return;

        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    private bool IsGrounded()
    {
        if (groundCheck == null)
            return true;

        return Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
    }
}
