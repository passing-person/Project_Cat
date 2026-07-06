using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ThirdPersonCameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private PlayerController playerController;

    [Header("Third Person Camera")]
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 0.75f, 0f);
    [SerializeField] private float distance = 3.2f;
    [SerializeField] private float minDistance = 1.2f;
    [SerializeField] private float maxDistance = 5f;
    [SerializeField] private float followSharpness = 18f;
    [SerializeField] private float rotationSharpness = 20f;
    [SerializeField] private float normalFov = 60f;

    [Header("Hidden Box Camera")]
    [SerializeField] private bool useFixedHideAnchorView = true;
    [SerializeField] private Vector3 hiddenEyeOffset = new Vector3(0f, 0.55f, 0.08f);
    [SerializeField] private float hiddenDistance = 0.05f;
    [SerializeField] private float hiddenFov = 42f;
    [SerializeField] private float hiddenYawLimit = 38f;
    [SerializeField] private float hiddenMinPitch = -12f;
    [SerializeField] private float hiddenMaxPitch = 22f;

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivityX = 3.2f;
    [SerializeField] private float mouseSensitivityY = 2.4f;
    [SerializeField] private float minPitch = -35f;
    [SerializeField] private float maxPitch = 70f;
    [SerializeField] private bool invertY;
    [SerializeField] private bool rotateTargetYaw = true;

    [Header("Collision")]
    [SerializeField] private bool useCameraCollision = true;
    [SerializeField] private float collisionRadius = 0.2f;
    [SerializeField] private LayerMask collisionMask = ~0;

    [Header("Cursor")]
    [SerializeField] private bool lockCursorOnStart = true;

    private Camera attachedCamera;
    private float yaw;
    private float pitch = 18f;
    private float hiddenYawCenter;
    private Transform hiddenViewAnchor;
    private bool wasHidden;
    private Vector3 currentVelocity;
    private float shakeTimer;
    private float shakeMagnitude;

    private void Awake()
    {
        attachedCamera = GetComponent<Camera>();

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

        if (attachedCamera != null)
        {
            normalFov = attachedCamera.fieldOfView;
        }

        if (lockCursorOnStart)
        {
            LockCursor();
        }
    }

    private void LateUpdate()
    {
        HandleCursor();

        if (GameInputGate.IsGameplayInputBlocked)
        {
            return;
        }

        if (target == null)
        {
            return;
        }

        bool isHidden = playerController != null && playerController.IsHidden;
        if (isHidden && !wasHidden)
        {
            EnterHiddenCameraMode();
        }
        wasHidden = isHidden;

        if (isHidden)
        {
            if (!(useFixedHideAnchorView && hiddenViewAnchor != null))
            {
                UpdateHiddenInput();
            }
            UpdateHiddenCameraPosition();
        }
        else
        {
            UpdateOrbitInput();
            UpdateThirdPersonCameraPosition();
        }
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

    public void SetHiddenViewAnchor(Transform anchor)
    {
        hiddenViewAnchor = anchor;
        if (anchor != null)
        {
            hiddenYawCenter = anchor.eulerAngles.y;
            yaw = hiddenYawCenter;
            pitch = NormalizePitch(anchor.eulerAngles.x);
            currentVelocity = Vector3.zero;
        }
    }

    public void AddImpulse(float magnitude, float duration)
    {
        shakeMagnitude = Mathf.Max(shakeMagnitude, magnitude);
        shakeTimer = Mathf.Max(shakeTimer, duration);
    }

    private void EnterHiddenCameraMode()
    {
        if (hiddenViewAnchor != null)
        {
            hiddenYawCenter = hiddenViewAnchor.eulerAngles.y;
            yaw = hiddenYawCenter;
            pitch = NormalizePitch(hiddenViewAnchor.eulerAngles.x);
        }
        else
        {
            hiddenYawCenter = target.eulerAngles.y;
            yaw = hiddenYawCenter;
            pitch = 0f;
        }
        currentVelocity = Vector3.zero;
    }

    private void UpdateOrbitInput()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivityX;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivityY;
        if (invertY) mouseY = -mouseY;

        yaw += mouseX;
        pitch = Mathf.Clamp(pitch - mouseY, minPitch, maxPitch);

        if (rotateTargetYaw && target != null)
        {
            Quaternion targetYaw = Quaternion.Euler(0f, yaw, 0f);
            target.rotation = Quaternion.Slerp(target.rotation, targetYaw, 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime));
        }
    }

    private void UpdateHiddenInput()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivityX;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivityY;
        if (invertY) mouseY = -mouseY;

        yaw = ClampAngleAroundCenter(yaw + mouseX, hiddenYawCenter, hiddenYawLimit);
        pitch = Mathf.Clamp(pitch - mouseY, hiddenMinPitch, hiddenMaxPitch);
    }

    private void UpdateThirdPersonCameraPosition()
    {
        if (attachedCamera != null)
        {
            attachedCamera.fieldOfView = Mathf.Lerp(attachedCamera.fieldOfView, normalFov, 1f - Mathf.Exp(-10f * Time.deltaTime));
        }

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 pivot = target.position + targetOffset;
        Vector3 desiredDirection = rotation * Vector3.back;
        float desiredDistance = distance;

        if (useCameraCollision)
        {
            if (Physics.SphereCast(pivot, collisionRadius, desiredDirection, out RaycastHit hit, distance, collisionMask, QueryTriggerInteraction.Ignore))
            {
                if (!hit.transform.IsChildOf(target))
                {
                    desiredDistance = Mathf.Clamp(hit.distance - 0.05f, minDistance, distance);
                }
            }
        }

        Vector3 desiredPosition = pivot + desiredDirection * desiredDistance + GetShakeOffset();
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, 1f / Mathf.Max(1f, followSharpness));
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(pivot - transform.position, Vector3.up), 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime));
    }

    private void UpdateHiddenCameraPosition()
    {
        if (attachedCamera != null)
        {
            attachedCamera.fieldOfView = Mathf.Lerp(attachedCamera.fieldOfView, hiddenFov, 1f - Mathf.Exp(-12f * Time.deltaTime));
        }

        Quaternion rotation;
        Vector3 desiredPosition;

        if (useFixedHideAnchorView && hiddenViewAnchor != null)
        {
            rotation = hiddenViewAnchor.rotation;
            desiredPosition = hiddenViewAnchor.position + GetShakeOffset() * 0.25f;
        }
        else
        {
            rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 pivot = target.position + hiddenEyeOffset;
            desiredPosition = pivot + rotation * Vector3.back * hiddenDistance + GetShakeOffset() * 0.4f;
        }

        transform.position = Vector3.Lerp(transform.position, desiredPosition, 1f - Mathf.Exp(-20f * Time.deltaTime));
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, 1f - Mathf.Exp(-20f * Time.deltaTime));
    }

    private Vector3 GetShakeOffset()
    {
        if (shakeTimer <= 0f)
        {
            return Vector3.zero;
        }

        shakeTimer -= Time.deltaTime;
        float falloff = Mathf.Clamp01(shakeTimer);
        Vector3 offset = Random.insideUnitSphere * shakeMagnitude * falloff;
        if (shakeTimer <= 0f)
        {
            shakeMagnitude = 0f;
        }
        return offset;
    }

    private static float NormalizePitch(float angle)
    {
        return Mathf.DeltaAngle(0f, angle);
    }

    private static float ClampAngleAroundCenter(float angle, float center, float limit)
    {
        float delta = Mathf.DeltaAngle(center, angle);
        delta = Mathf.Clamp(delta, -limit, limit);
        return center + delta;
    }

    private void HandleCursor()
    {
        if (GameInputGate.IsGameplayInputBlocked)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
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
