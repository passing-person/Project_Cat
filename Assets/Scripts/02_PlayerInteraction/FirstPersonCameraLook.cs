using UnityEngine;

[DefaultExecutionOrder(100)]
public class FirstPersonCameraLook : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private PlayerController playerController;

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 2.5f;
    [SerializeField] private float minPitch = -75f;
    [SerializeField] private float maxPitch = 75f;
    [SerializeField] private bool invertY = false;

    [Header("Cursor")]
    [SerializeField] private bool lockCursorOnStart = true;

    private float pitch;
    private float yaw;
    private Rigidbody rb;

    private void Awake()
    {
        if (playerController == null)
            playerController = GetComponent<PlayerController>();

        if (cameraTransform == null)
        {
            Camera childCamera = GetComponentInChildren<Camera>();
            if (childCamera != null)
                cameraTransform = childCamera.transform;
            else if (Camera.main != null)
                cameraTransform = Camera.main.transform;
        }

        rb = GetComponent<Rigidbody>();
        yaw = transform.eulerAngles.y;

        DisableConflictingCameraControllers();

        if (lockCursorOnStart)
            LockCursor();
    }

    private void Update()
    {
        HandleCursor();

        if (!CanLook())
            return;

        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

        if (invertY)
            mouseY = -mouseY;

        yaw += mouseX;

        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    private void LateUpdate()
    {
        if (!CanLook())
            return;

        ApplyLookRotation();
    }

    private bool CanLook()
    {
        if (cameraTransform == null)
            return false;

        if (playerController == null)
            return true;

        return playerController.IsControllable && !playerController.IsHidden;
    }

    private void HandleCursor()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
            LockCursor();
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void ApplyLookRotation()
    {
        Quaternion bodyRotation = Quaternion.Euler(0f, yaw, 0f);
        transform.rotation = bodyRotation;

        if (rb != null)
            rb.rotation = bodyRotation;

        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void DisableConflictingCameraControllers()
    {
        MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null || behaviour == this)
                continue;

            if (behaviour.GetType().Name == "SimpleCameraController")
                behaviour.enabled = false;
        }
    }
}
