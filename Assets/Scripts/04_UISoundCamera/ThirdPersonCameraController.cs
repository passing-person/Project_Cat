using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ThirdPersonCameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private PlayerController playerController;

    [Header("Camera")]
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 0.65f, 0f);
    [SerializeField] private float distance = 3.2f;
    [SerializeField] private float minDistance = 1.2f;
    [SerializeField] private float maxDistance = 5f;
    [SerializeField] private float followSharpness = 18f;
    [SerializeField] private float rotationSharpness = 20f;

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivityX = 3.2f;
    [SerializeField] private float mouseSensitivityY = 2.4f;
    [SerializeField] private float minPitch = -30f;
    [SerializeField] private float maxPitch = 65f;
    [SerializeField] private bool invertY;
    [SerializeField] private bool rotateTargetYaw = true;

    [Header("Collision")]
    [SerializeField] private bool useCameraCollision = true;
    [SerializeField] private float collisionRadius = 0.2f;
    [SerializeField] private LayerMask collisionMask = ~0;

    [Header("Cursor")]
    [SerializeField] private bool lockCursorOnStart = true;

    private float yaw;
    private float pitch = 18f;
    private Vector3 currentVelocity;

    private void Awake()
    {
        if (target == null)
        {
            PlayerController foundPlayer = FindObjectOfType<PlayerController>();
            if (foundPlayer != null)
            {
                target = foundPlayer.transform;
                playerController = foundPlayer;
            }
        }

        if (playerController == null && target != null)
        {
            playerController = target.GetComponent<PlayerController>();
        }

        if (target != null)
        {
            yaw = target.eulerAngles.y;
        }

        if (lockCursorOnStart)
        {
            LockCursor();
        }
    }

    private void LateUpdate()
    {
        HandleCursor();

        if (target == null)
        {
            return;
        }

        bool canLook = playerController == null || (playerController.IsControllable && !playerController.IsHidden);
        if (canLook)
        {
            UpdateOrbitInput();
        }

        UpdateCameraPosition();
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        playerController = target != null ? target.GetComponent<PlayerController>() : null;
        if (target != null)
        {
            yaw = target.eulerAngles.y;
        }
    }

    public void SetDistance(float newDistance)
    {
        distance = Mathf.Clamp(newDistance, minDistance, maxDistance);
    }

    private void UpdateOrbitInput()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivityX;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivityY;

        if (invertY)
        {
            mouseY = -mouseY;
        }

        yaw += mouseX;
        pitch = Mathf.Clamp(pitch - mouseY, minPitch, maxPitch);

        if (rotateTargetYaw && target != null)
        {
            Quaternion targetYaw = Quaternion.Euler(0f, yaw, 0f);
            target.rotation = Quaternion.Slerp(target.rotation, targetYaw, 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime));
        }
    }

    private void UpdateCameraPosition()
    {
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 pivot = target.position + targetOffset;
        Vector3 desiredDirection = rotation * Vector3.back;
        float desiredDistance = distance;

        if (useCameraCollision)
        {
            RaycastHit hit;
            if (Physics.SphereCast(pivot, collisionRadius, desiredDirection, out hit, distance, collisionMask, QueryTriggerInteraction.Ignore))
            {
                if (!hit.transform.IsChildOf(target))
                {
                    desiredDistance = Mathf.Clamp(hit.distance - 0.05f, minDistance, distance);
                }
            }
        }

        Vector3 desiredPosition = pivot + desiredDirection * desiredDistance;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, 1f / Mathf.Max(1f, followSharpness));
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(pivot - transform.position, Vector3.up), 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime));
    }

    private void HandleCursor()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
        {
            LockCursor();
        }
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
